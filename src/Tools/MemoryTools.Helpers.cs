using Maenifold.Utils;
using System.Text.RegularExpressions;

namespace Maenifold.Tools;

public partial class MemoryTools
{
    private static string BasePath => Config.MemoryPath;

    static MemoryTools()
    {
        Config.EnsureDirectories();
    }

    private static string PathToUri(string path) => MarkdownIO.PathToUri(path, BasePath);

    private static string UriToPath(string uri) => MarkdownIO.UriToPath(uri, BasePath);

    private static string? FindFileByTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return null;

        var normalized = title.Replace('\\', '/').Trim('/');
        if (normalized.Length == 0)
            return null;


        var relativePath = normalized.Replace('/', Path.DirectorySeparatorChar);
        var directPath = Path.Combine(BasePath, relativePath);

        if (File.Exists(directPath))
            return directPath;

        if (!Path.HasExtension(relativePath))
        {
            var mdPath = directPath + ".md";
            if (File.Exists(mdPath))
                return mdPath;
        }
        else if (!directPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            var withMdExtension = Path.Combine(BasePath, Path.ChangeExtension(relativePath, ".md"));
            if (File.Exists(withMdExtension))
                return withMdExtension;
        }


        var files = Directory.GetFiles(BasePath, "*.md", SearchOption.AllDirectories);
        var slug = MarkdownIO.Slugify(title);

        return files.FirstOrDefault(f =>
            Path.GetFileNameWithoutExtension(f).Equals(slug, StringComparison.OrdinalIgnoreCase));
    }

    private static string PerformFindReplace(string content, string findText, string replaceText, int? expectedCount)
    {
        var matches = content.Split(findText).Length - 1;
        if (expectedCount.HasValue && matches != expectedCount)
            throw new InvalidOperationException($"Expected {expectedCount} matches but found {matches}");

        return content.Replace(findText, replaceText);
    }

    private static string ReplaceSection(string content, string sectionName, string newContent)
    {
        var pattern = $@"(^|\n)(#+\s*{Regex.Escape(sectionName)}.*?)(?=\n#|\z)";
        var replacement = $"$1$2\n{newContent}";
        return Regex.Replace(
            content,
            pattern,
            replacement,
            RegexOptions.Multiline | RegexOptions.Singleline);
    }

    private static string? ValidatePathSecurity(string folderPath)
    {

        if (Path.IsPathRooted(folderPath))
        {
            return "Absolute paths are not allowed. Use relative paths only.";
        }


        var normalizedPath = folderPath.Replace('\\', '/');


        var segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            if (segment == ".." || segment == ".")
            {
                return "Path traversal components ('.' and '..') are not allowed.";
            }
        }

        return null;
    }

    private static string SanitizeUserInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "Untitled";


        if (input.Length > 255)
            input = input.Substring(0, 255);


        input = Regex.Replace(input, @"<[^>]*>", "", RegexOptions.IgnoreCase | RegexOptions.Compiled);


        input = Regex.Replace(input, @"[:|>\-\[\]{}#&*!@`']", "", RegexOptions.Compiled);


        var dangerousChars = new char[] { '$', '(', ')', ';', '&', '|', '`', '\\', '<', '>', '"' };
        foreach (var ch in dangerousChars)
        {
            input = input.Replace(ch.ToString(), "");
        }


        input = input.Replace("..", "").Replace("/", "").Replace("\\", "");


        input = Regex.Replace(input, @"[\x00-\x1F\x7F-\x9F]", "", RegexOptions.Compiled);


        input = Regex.Replace(input, @"\s+", " ", RegexOptions.Compiled).Trim();


        if (string.IsNullOrWhiteSpace(input))
            return "Untitled";


        var lowerInput = input.ToLowerInvariant();
        if (lowerInput.Contains("javascript") ||
            lowerInput.Contains("vbscript") ||
            lowerInput.Contains("onload") ||
            lowerInput.Contains("onerror") ||
            lowerInput.Contains("eval") ||
            lowerInput.Contains("expression"))
        {
            return "Untitled";
        }

        return input;
    }
}