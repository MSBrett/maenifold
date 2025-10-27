using System.Diagnostics;
using Maenifold.Utils;

namespace Maenifold.Tools;

public partial class MemorySearchTools
{
    private static List<(string path, double fusedScore, double textScore, double semanticScore, string title, string snippet)>
        ApplyReciprocalRankFusion(
            List<(string path, double score)> vectorResults,
            List<(string path, double score)> textResults,
            string query,
            int k = 60)
    {
        var fusionTimer = Stopwatch.StartNew();
        var rrfScores = new Dictionary<string, double>();
        var originalScores = new Dictionary<string, (double text, double semantic)>();
        var fileInfo = new Dictionary<string, (string title, string snippet)>();


        for (int i = 0; i < vectorResults.Count; i++)
        {
            var path = vectorResults[i].path;
            var rrfScore = 1.0 / (k + i + 1);
            rrfScores[path] = rrfScores.GetValueOrDefault(path, 0.0) + rrfScore;


            if (!originalScores.TryGetValue(path, out var existingScore))
                originalScores[path] = (0.0, vectorResults[i].score);
            else
                originalScores[path] = (originalScores[path].text, vectorResults[i].score);


            if (!fileInfo.ContainsKey(path))
            {
                var (title, snippet) = GetFileDisplayInfo(path, query);
                fileInfo[path] = (title, snippet);
            }
        }


        for (int i = 0; i < textResults.Count; i++)
        {
            var path = textResults[i].path;
            var rrfScore = 1.0 / (k + i + 1);
            rrfScores[path] = rrfScores.GetValueOrDefault(path, 0.0) + rrfScore;


            var normalizedTextScore = Math.Min(textResults[i].score / 100.0, 1.0);
            if (!originalScores.TryGetValue(path, out var existingTextScore))
                originalScores[path] = (normalizedTextScore, 0.0);
            else
                originalScores[path] = (normalizedTextScore, originalScores[path].semantic);


            if (!fileInfo.ContainsKey(path))
            {
                var (title, snippet) = GetFileDisplayInfo(path, query);
                fileInfo[path] = (title, snippet);
            }
        }


        var fusedResults = rrfScores
                    .OrderByDescending(kv => kv.Value)
                    .Select(kv =>
                    {
                        var (title, snippet) = fileInfo.GetValueOrDefault(kv.Key, (Path.GetFileNameWithoutExtension(kv.Key), ""));
                        var (textScore, semanticScore) = originalScores.GetValueOrDefault(kv.Key, (0.0, 0.0));
                        return (kv.Key, kv.Value, textScore, semanticScore, title, snippet);
                    })
                    .ToList();

        fusionTimer.Stop();
        if (Config.EnableVectorSearchLogs)
            Console.Error.WriteLine($"[HYBRID SEARCH] Text: {textResults.Count}, Vector: {vectorResults.Count}, Fused: {fusedResults.Count}, Time: {fusionTimer.ElapsedMilliseconds}ms");

        return fusedResults;
    }

    private static (string title, string snippet) GetFileDisplayInfo(string filePathOrUri, string query)
    {
        try
        {

            string filePath;
            if (filePathOrUri.StartsWith("memory://", StringComparison.Ordinal))
            {
                filePath = MarkdownIO.UriToPath(filePathOrUri, Config.MemoryPath);
            }
            else
            {
                filePath = filePathOrUri;
            }

            var (frontmatter, content, _) = MarkdownIO.ReadMarkdown(filePath);
            var title = frontmatter?.ContainsKey("title") == true
                ? frontmatter["title"].ToString()
                : Path.GetFileNameWithoutExtension(filePath);
            var snippet = ExtractSnippet(content, query, 200);
            return (title!, snippet);
        }
        catch
        {

            var name = filePathOrUri.StartsWith("memory://", StringComparison.Ordinal)
                            ? filePathOrUri.Replace("memory://", "").Replace('/', Path.DirectorySeparatorChar)
                            : filePathOrUri;
            return (Path.GetFileNameWithoutExtension(name), "");
        }
    }
}
