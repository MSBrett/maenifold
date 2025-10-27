using System.Text.RegularExpressions;
using Maenifold.Utils;

namespace Maenifold.Tools;

public static class ConceptAnalyzer
{
    private static readonly Regex WikiLinkPattern = new(@"\[\[([^\]]+)\]\]", RegexOptions.Compiled);

    private static string? ClassifyPathPattern(string concept)
    {
        if (concept.StartsWith('/') || concept.Contains('.'))
            return "File Paths";
        return null;
    }

    private static string? ClassifyPluralPattern(string concept)
    {
        if (concept.EndsWith("s", StringComparison.OrdinalIgnoreCase) && !concept.EndsWith("ss", StringComparison.OrdinalIgnoreCase))
            return "Singular/Plural";
        return null;
    }

    private static string? ClassifyCompoundPattern(string concept)
    {
        if (concept.Contains('-'))
            return "Compound (with -)";
        return null;
    }

    private static string? ClassifySuffixPattern(string concept)
    {
        if (concept.Contains("system", StringComparison.OrdinalIgnoreCase) ||
            concept.Contains("framework", StringComparison.OrdinalIgnoreCase) ||
            concept.Contains("architecture", StringComparison.OrdinalIgnoreCase) ||
            concept.Contains("tool", StringComparison.OrdinalIgnoreCase))
            return "With Suffix";
        return null;
    }

    private static string ClassifyConceptPattern(string concept)
    {
        var pathPattern = ClassifyPathPattern(concept);
        if (pathPattern != null)
            return pathPattern;

        var pluralPattern = ClassifyPluralPattern(concept);
        if (pluralPattern != null)
            return pluralPattern;

        var compoundPattern = ClassifyCompoundPattern(concept);
        if (compoundPattern != null)
            return compoundPattern;

        var suffixPattern = ClassifySuffixPattern(concept);
        if (suffixPattern != null)
            return suffixPattern;

        return "Other";
    }

    public static string AnalyzeConceptCorruption(string conceptFamily, int maxResults = 50)
    {
        var memoryPath = Config.MemoryPath;
        var conceptCounts = new Dictionary<string, int>();


        var mdFiles = Directory.GetFiles(memoryPath, "*.md", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("/.git/", StringComparison.OrdinalIgnoreCase) && !f.Contains("\\.git\\", StringComparison.OrdinalIgnoreCase))
                    .ToList();

        foreach (var file in mdFiles)
        {
            try
            {
                var content = File.ReadAllText(file);
                var matches = WikiLinkPattern.Matches(content);

                foreach (Match match in matches)
                {
                    var concept = match.Groups[1].Value;
                    var normalized = MarkdownIO.NormalizeConcept(concept);

                    if (normalized.Contains(conceptFamily.ToLowerInvariant(), StringComparison.Ordinal))
                    {
                        if (!conceptCounts.ContainsKey(concept))
                            conceptCounts[concept] = 0;
                        conceptCounts[concept]++;
                    }
                }
            }
            catch
            {

            }
        }

        var results = new List<string>();
        results.Add($"=== Concept Family Analysis: '{conceptFamily}' ===");
        results.Add($"Found {conceptCounts.Count} unique variants");
        results.Add("");


        var patterns = new Dictionary<string, List<string>>();
        patterns["Singular/Plural"] = new List<string>();
        patterns["Compound (with -)"] = new List<string>();
        patterns["With Suffix"] = new List<string>();
        patterns["File Paths"] = new List<string>();
        patterns["Other"] = new List<string>();

        foreach (var kvp in conceptCounts.OrderByDescending(x => x.Value).Take(maxResults))
        {
            var concept = kvp.Key;
            var patternType = ClassifyConceptPattern(concept);
            patterns[patternType].Add($"{concept} ({kvp.Value}x)");
        }

        foreach (var pattern in patterns.Where(p => p.Value.Any()))
        {
            results.Add($"{pattern.Key}:");
            results.AddRange(pattern.Value.Select(v => $"  • {v}"));
            results.Add("");
        }


        results.Add("=== SUGGESTED REPAIRS ===");

        var plurals = conceptCounts.Keys
                    .Where(c => c.EndsWith("s", StringComparison.OrdinalIgnoreCase) && !c.EndsWith("ss", StringComparison.OrdinalIgnoreCase))
                    .Select(c => c)
                    .ToList();

        if (plurals.Any())
        {
            var singular = conceptFamily.TrimEnd('s');
            results.Add($"Fix plural forms:");
            results.Add($"  RepairConcepts conceptsToReplace='{string.Join(",", plurals.Take(10))}' canonicalConcept='{singular}'");
            results.Add("");
        }

        var compounds = conceptCounts.Keys
                    .Where(c => c.Contains($"-{conceptFamily}", StringComparison.OrdinalIgnoreCase) || c.Contains($"{conceptFamily}-", StringComparison.OrdinalIgnoreCase))
                    .ToList();

        if (compounds.Any())
        {
            results.Add($"Fix compound forms:");
            results.Add($"  RepairConcepts conceptsToReplace='{string.Join(",", compounds.Take(10))}' canonicalConcept='{conceptFamily}'");
        }

        return string.Join("\n", results);
    }
}
