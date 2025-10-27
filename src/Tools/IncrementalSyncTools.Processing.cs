using Microsoft.Data.Sqlite;
using Maenifold.Utils;

namespace Maenifold.Tools;

public partial class IncrementalSyncTools
{
    private static void SyncFile(string fullPath)
    {
        try
        {
            var memoryUri = PathToUri(fullPath);
            var (frontmatter, content, _) = MarkdownIO.ReadMarkdown(fullPath);

            if (Config.EnableSessionCleanup && frontmatter != null)
            {
                SessionCleanup.HandleSessionCleanup(frontmatter!, fullPath, content);
                (frontmatter, content, _) = MarkdownIO.ReadMarkdown(fullPath);
            }

            var title = frontmatter?.TryGetValue("title", out var titleValue) == true && !string.IsNullOrWhiteSpace(titleValue?.ToString())
                ? titleValue!.ToString()!
                : Path.GetFileNameWithoutExtension(fullPath);
            var status = frontmatter?.TryGetValue("status", out var statusValue) == true ? statusValue?.ToString() : null;
            var createdUtc = File.GetCreationTimeUtc(fullPath);
            var concepts = MarkdownIO.ExtractWikiLinks(content);

            using var conn = new SqliteConnection(Config.DatabaseConnectionString);
            conn.OpenWithWAL();

            UpsertFileContent(conn, memoryUri, title, content, status);
            UpsertConceptMetadata(conn, memoryUri, content, concepts, createdUtc);
            UpdateConceptRelations(conn, concepts, memoryUri);
            UpdateVectorState(conn, memoryUri, content, concepts);
            UpdateFullTextIndex(conn, memoryUri);

            conn.Close();

            LogSync($"Incremental sync applied for {memoryUri} (concepts: {concepts.Count}).");
            ScheduleMaintenanceIfNeeded();
        }
        catch (Exception ex)
        {
            LogSync($"Failed to process incremental sync for '{fullPath}'.", ex);
        }
    }

    private static void ProcessFileCreated(string fullPath) => SyncFile(fullPath);

    private static void ProcessFileChanged(string fullPath) => SyncFile(fullPath);

    private static void ProcessFileDeleted(string fullPath)
    {
        try
        {
            var memoryUri = PathToUri(fullPath);

            using var conn = new SqliteConnection(Config.DatabaseConnectionString);
            conn.OpenWithWAL();

            var rowId = GetFileRowId(conn, memoryUri);

            conn.Execute("DELETE FROM concept_mentions WHERE source_file = @file", new { file = memoryUri });
            RemoveFileFromGraph(conn, memoryUri);
            RemoveVectorState(conn, memoryUri);
            if (rowId.HasValue)
            {
                RemoveFullTextIndex(conn, rowId.Value);
            }
            conn.Execute("DELETE FROM file_content WHERE file_path = @file", new { file = memoryUri });

            conn.Close();

            LogSync($"Incremental sync removed {memoryUri}.");
            ScheduleMaintenanceIfNeeded();
        }
        catch (Exception ex)
        {
            LogSync($"Failed to process deletion for '{fullPath}'.", ex);
        }
    }

    private static void ProcessFileRenamed(string oldPath, string newPath)
    {
        try
        {
            ProcessFileDeleted(oldPath);
            SyncFile(newPath);
        }
        catch (Exception ex)
        {
            LogSync($"Failed to process rename from '{oldPath}' to '{newPath}'.", ex);
        }
    }
}
