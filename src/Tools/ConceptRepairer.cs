using System.Text.RegularExpressions;
using Maenifold.Utils;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Maenifold.Tools;

public static class ConceptRepairer
{
    private static readonly Regex WikiLinkPattern = new(@"\[\[([^\]]+)\]\]", RegexOptions.Compiled);
    private static readonly char[] SplitChars = { ',', ';', '|' };

    private static void ProcessInline(Inline inline, Regex pattern, string variant, string canonicalConcept,
            string fullContent, List<(int start, int length, string oldText, string newText)> replacements)
    {
        if (inline is CodeInline or LinkInline) return;

        if (inline is LiteralInline literal)
        {
            foreach (Match match in pattern.Matches(literal.Content.ToString()))
            {
                var pos = literal.Span.Start + match.Index;
                var before = pos >= 2 ? fullContent.Substring(pos - 2, 2) : "";
                var after = pos + match.Length + 1 < fullContent.Length ? fullContent.Substring(pos + match.Length, 2) : "";
                if (before != "[[" && after != "]]")
                    replacements.Add((pos, match.Length, match.Value, $"[[{canonicalConcept}]]"));
            }
        }

        if (inline is ContainerInline container)
            foreach (var child in container)
                ProcessInline(child, pattern, variant, canonicalConcept, fullContent, replacements);
    }

    private static void ProcessBlockInlines(MarkdownObject node, Regex pattern, string variant,
            string canonicalConcept, string content, List<(int, int, string, string)> replacements)
    {
        if (node is ParagraphBlock paragraph && paragraph.Inline != null)
            foreach (var inline in paragraph.Inline)
                ProcessInline(inline, pattern, variant, canonicalConcept, content, replacements);
        else if (node is HeadingBlock heading && heading.Inline != null)
            foreach (var inline in heading.Inline)
                ProcessInline(inline, pattern, variant, canonicalConcept, content, replacements);
        else if (node is ListItemBlock listItem)
            foreach (var child in listItem)
                if (child is ParagraphBlock para && para.Inline != null)
                    foreach (var inline in para.Inline)
                        ProcessInline(inline, pattern, variant, canonicalConcept, content, replacements);
    }

    private static void ProcessCreateWikiLinks(string content, HashSet<string> variants, string canonicalConcept,
            List<string> replacements, ref string newContent, ref int totalReplacements)
    {
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var document = Markdown.Parse(content, pipeline);
        var textReplacements = new List<(int start, int length, string oldText, string newText)>();

        foreach (var variant in variants)
        {
            var pattern = new Regex($@"\b{Regex.Escape(variant)}\b", RegexOptions.IgnoreCase);
            foreach (var node in document.Descendants())
                if (node is not (CodeBlock or FencedCodeBlock))
                    ProcessBlockInlines(node, pattern, variant, canonicalConcept, content, textReplacements);
        }

        textReplacements.Sort((a, b) => b.start.CompareTo(a.start));
        foreach (var (start, length, oldText, newText) in textReplacements)
        {
            newContent = string.Concat(newContent.AsSpan(0, start), newText, newContent.AsSpan(start + length));
            replacements.Add($"  {oldText} → {newText}");
            totalReplacements++;
        }
    }

    public static string RepairConcepts(
            string conceptsToReplace,
            string canonicalConcept,
            string? folder = null,
            bool dryRun = true,
            bool createWikiLinks = false)
    {
        var memoryPath = Config.MemoryPath;
        var searchPath = string.IsNullOrEmpty(folder)
            ? memoryPath
            : Path.Combine(memoryPath, folder);

        var validationError = ValidateInputs(searchPath, conceptsToReplace, canonicalConcept, createWikiLinks);
        if (validationError != null)
        {
            return validationError;
        }

        var variants = ParseConcepts(conceptsToReplace);
        var (removingBrackets, normalizedCanonical) = GetNormalizationInfo(canonicalConcept);
        var mdFiles = DiscoverMarkdownFiles(searchPath);

        var results = new List<string>();
        BuildHeaderLines(results, mdFiles.Count, variants, canonicalConcept, normalizedCanonical, createWikiLinks, removingBrackets);

        var totalReplacements = 0;
        var filesModified = 0;

        ProcessFiles(mdFiles, memoryPath, variants, canonicalConcept, createWikiLinks, removingBrackets,
                     dryRun, results, ref totalReplacements, ref filesModified);

        BuildSummaryLines(results, mdFiles.Count, filesModified, totalReplacements, dryRun);

        return string.Join("\n", results);
    }

    private static string? ValidateInputs(string searchPath, string conceptsToReplace, string canonicalConcept, bool createWikiLinks)
    {
        if (!Directory.Exists(searchPath))
        {
            return $"ERROR: Directory not found: {searchPath}";
        }

        var variants = ParseConcepts(conceptsToReplace);
        if (!variants.Any())
        {
            return "ERROR: No concepts to replace provided";
        }

        if (createWikiLinks && string.IsNullOrEmpty(canonicalConcept))
        {
            return "ERROR: Cannot both create WikiLinks and remove them (createWikiLinks=true with empty canonicalConcept)";
        }

        return null;
    }

    private static HashSet<string> ParseConcepts(string conceptsToReplace)
    {
        return conceptsToReplace
            .Split(SplitChars, StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim().ToLowerInvariant())
            .Where(c => !string.IsNullOrEmpty(c))
            .ToHashSet();
    }

    private static (bool removingBrackets, string normalizedCanonical) GetNormalizationInfo(string canonicalConcept)
    {
        var removingBrackets = string.IsNullOrEmpty(canonicalConcept);
        var normalizedCanonical = removingBrackets ? "" : MarkdownIO.NormalizeConcept(canonicalConcept);
        return (removingBrackets, normalizedCanonical);
    }

    private static List<string> DiscoverMarkdownFiles(string searchPath)
    {
        return Directory.GetFiles(searchPath, "*.md", SearchOption.AllDirectories)
            .Where(f => !f.Contains("/.git/", StringComparison.OrdinalIgnoreCase) && !f.Contains("\\.git\\", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f)
            .ToList();
    }

    private static void BuildHeaderLines(List<string> results, int fileCount, HashSet<string> variants,
                                         string canonicalConcept, string normalizedCanonical,
                                         bool createWikiLinks, bool removingBrackets)
    {
        results.Add($"Scanning {fileCount} markdown files...");
        if (createWikiLinks)
        {
            results.Add($"Looking for plain text: {string.Join(", ", variants)}");
            results.Add($"Will CREATE WikiLinks: text → [[{canonicalConcept}]]");
        }
        else
        {
            results.Add($"Looking for variants: {string.Join(", ", variants)}");
            if (removingBrackets)
                results.Add($"Will REMOVE WikiLink brackets entirely (converting to plain text)");
            else
                results.Add($"Will replace with: [[{canonicalConcept}]] (normalized: {normalizedCanonical})");
        }
        results.Add("");
    }

    private static void ProcessFiles(List<string> mdFiles, string memoryPath, HashSet<string> variants,
                                     string canonicalConcept, bool createWikiLinks, bool removingBrackets,
                                     bool dryRun, List<string> results, ref int totalReplacements,
                                     ref int filesModified)
    {
        foreach (var file in mdFiles)
        {
            try
            {
                var content = File.ReadAllText(file);
                var replacements = new List<string>();
                var newContent = content;

                if (createWikiLinks)
                {
                    ProcessCreateWikiLinks(newContent, variants, canonicalConcept, replacements, ref newContent, ref totalReplacements);
                }
                else
                {
                    ProcessWikiLinkReplacements(content, variants, canonicalConcept, removingBrackets,
                                               replacements, ref newContent, ref totalReplacements);
                }

                if (replacements.Any())
                {
                    var relativePath = Path.GetRelativePath(memoryPath, file);
                    filesModified++;

                    if (!dryRun)
                    {
                        File.WriteAllText(file, newContent);
                        results.Add($"✓ Modified: {relativePath}");
                    }
                    else
                    {
                        results.Add($"Would modify: {relativePath}");
                    }

                    results.AddRange(replacements);
                }
            }
            catch (Exception ex)
            {
                results.Add($"ERROR processing {file}: {ex.Message}");
            }
        }
    }

    private static void ProcessWikiLinkReplacements(string content, HashSet<string> variants, string canonicalConcept,
                                                    bool removingBrackets, List<string> replacements,
                                                    ref string newContent, ref int totalReplacements)
    {
        var matches = WikiLinkPattern.Matches(content);

        foreach (Match match in matches)
        {
            var concept = match.Groups[1].Value;
            var normalizedConcept = MarkdownIO.NormalizeConcept(concept);

            if (variants.Contains(normalizedConcept))
            {
                var oldLink = match.Value;
                var newLink = removingBrackets
                    ? concept
                    : $"[[{canonicalConcept}]]";

                if (oldLink != newLink)
                {
                    newContent = newContent.Replace(oldLink, newLink);
                    replacements.Add($"  {oldLink} → {newLink}");
                    totalReplacements++;
                }
            }
        }
    }

    private static void BuildSummaryLines(List<string> results, int fileCount, int filesModified,
                                          int totalReplacements, bool dryRun)
    {
        results.Add("");
        results.Add("=== SUMMARY ===");
        results.Add($"Files scanned: {fileCount}");
        results.Add($"Files {(dryRun ? "to modify" : "modified")}: {filesModified}");
        results.Add($"Total replacements: {totalReplacements}");

        if (dryRun && filesModified > 0)
        {
            results.Add("");
            results.Add("This was a DRY RUN. To apply changes, run with dryRun=false");
            results.Add("");
            results.Add("After applying changes, run 'sync' to rebuild the graph with clean concepts.");
        }
        else if (!dryRun && filesModified > 0)
        {
            results.Add("");
            results.Add("✓ Changes applied successfully!");
            results.Add("Run 'sync' to rebuild the graph with the cleaned concepts.");
        }
    }
}
