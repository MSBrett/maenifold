using System.Diagnostics;
using Maenifold.Utils;
using Microsoft.Data.Sqlite;

namespace Maenifold.Tools;

public partial class MemorySearchTools
{
    private static (byte[] blob, long embeddingMs) PrepareVectorQuery(string query)
    {
        var embeddingTimer = Stopwatch.StartNew();
        var queryEmbedding = VectorTools.GenerateEmbedding(query);
        var embeddingBlob = VectorTools.ToSqliteVectorBlob(queryEmbedding);
        embeddingTimer.Stop();
        return (embeddingBlob, embeddingTimer.ElapsedMilliseconds);
    }

    private static List<(string path, double distance)> FilterResults(List<(string path, double distance)> dbResults, string? folderPathNormalized, string[]? tags)
    {
        var filtered = new List<(string path, double distance)>();
        foreach (var (fileUri, distance) in dbResults)
        {
            // Filter by folder if specified
            if (folderPathNormalized != null)
            {
                // Convert URI to path and check if it's in the specified search folder
                var filePath = MarkdownIO.UriToPath(fileUri, Config.MemoryPath);
                var normalizedFilePath = Path.GetFullPath(filePath);
                if (!normalizedFilePath.StartsWith(folderPathNormalized, StringComparison.Ordinal))
                    continue;
            }

            if (tags != null && tags.Length > 0)
            {
                if (IsFileFilteredByTags(fileUri, tags))
                    continue;
            }

            filtered.Add((fileUri, distance));
        }

        return filtered;
    }

    private static List<(string path, double score)> NormalizeScores(List<(string path, double distance)> filtered)
    {
        if (filtered.Count == 0)
            return new List<(string path, double score)>();

        var minDistance = filtered.Min(r => r.distance);
        var maxDistance = filtered.Max(r => r.distance);

        var results = new List<(string path, double score)>();
        foreach (var (fileUri, distance) in filtered)
        {
            double score;
            if (Math.Abs(maxDistance - minDistance) < 1e-12)
            {
                score = 1.0;
            }
            else
            {
                score = 1.0 - ((distance - minDistance) / (maxDistance - minDistance));
            }

            results.Add((fileUri, score));
        }

        return results;
    }

    private static List<(string path, double score)> GetVectorSearchResults(string query, string searchPath, int maxResults, string[]? tags)
    {
        var totalTimer = Stopwatch.StartNew();
        try
        {
            var (embeddingBlob, embeddingMs) = PrepareVectorQuery(query);

            if (embeddingBlob.Length == 0)
            {
                if (Config.EnableVectorSearchLogs)
                    Console.Error.WriteLine($"[VECTOR SEARCH] Query embedding: {embeddingMs}ms, Results: 0 (empty embedding)");
                return new List<(string path, double score)>();
            }

            var dbResults = new List<(string path, double distance)>();

            using var connection = new SqliteConnection(Config.DatabaseConnectionString);
            connection.OpenReadOnlyWithVector();
            string? folderUriPrefix = null;
            string? folderPathNormalized = null;
            if (!string.IsNullOrEmpty(searchPath) && !string.Equals(searchPath, Config.MemoryPath, StringComparison.Ordinal))
            {
                folderPathNormalized = Path.GetFullPath(searchPath).TrimEnd(Path.DirectorySeparatorChar);
                var relativeFolder = Path.GetRelativePath(Config.MemoryPath, folderPathNormalized);
                if (!string.IsNullOrEmpty(relativeFolder) && relativeFolder != ".")
                {
                    relativeFolder = relativeFolder.Replace(Path.DirectorySeparatorChar, '/');
                    folderUriPrefix = $"memory://{relativeFolder.TrimEnd('/')}/";
                }
            }

            var maxCandidateResults = Math.Min(maxResults * 5, 200);

            var sql = @"
                SELECT file_path, vec_distance_cosine(embedding, @embedding) as distance
                FROM vec_memory_files
                WHERE embedding IS NOT NULL";

            if (folderUriPrefix != null)
                sql += " AND file_path LIKE @folderPrefix || '%'";

            sql += @"
                ORDER BY distance
                LIMIT @maxResults";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.Add("@embedding", SqliteType.Blob).Value = embeddingBlob;
            command.Parameters.Add("@maxResults", SqliteType.Integer).Value = maxCandidateResults;
            if (folderUriPrefix != null)
            {
                command.Parameters.Add("@folderPrefix", SqliteType.Text).Value = folderUriPrefix;
            }

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var fileUri = reader.GetString(0);

                if (reader.IsDBNull(1))
                    continue;

                var distance = reader.GetDouble(1);
                dbResults.Add((fileUri, distance));
            }

            var filtered = FilterResults(dbResults, folderPathNormalized, tags);
            var results = NormalizeScores(filtered);

            totalTimer.Stop();
            if (Config.EnableVectorSearchLogs)
                Console.Error.WriteLine($"[VECTOR SEARCH] Query embedding: {embeddingMs}ms, Results: {results.Count}");
            return results;
        }
        catch (Exception ex)
        {
            totalTimer.Stop();

            if (Config.EnableVectorSearchLogs)
            {
                Console.Error.WriteLine($"Vector search error: {ex.Message}");
                Console.Error.WriteLine($"[VECTOR SEARCH] Query embedding: 0ms, Results: 0 (error)");
            }

            return new List<(string path, double score)>();
        }
    }

    private static bool IsFileFilteredByTags(string fileUri, string[] tags)
    {
        try
        {
            // Convert URI back to file path and read frontmatter
            var filePath = MarkdownIO.UriToPath(fileUri, Config.MemoryPath);

            var (frontmatter, _, _) = MarkdownIO.ReadMarkdown(filePath);
            var fileTags = frontmatter?.ContainsKey("tags") == true
                ? (frontmatter["tags"] as IEnumerable<object>)?.Select(t => t.ToString()).ToArray() ?? Array.Empty<string>()
                : Array.Empty<string>();

            // Return true (filter out) if file does NOT contain all required tags
            // Return false (keep file) if file contains all required tags
            return !tags.All(t => fileTags.Contains(t, StringComparer.OrdinalIgnoreCase));
        }
        catch
        {
            return true;  // Filter out files we can't read
        }
    }
}
