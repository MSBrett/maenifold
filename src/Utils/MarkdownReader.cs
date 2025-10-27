using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using YamlDotNet.Serialization;

namespace Maenifold.Utils;

public static class MarkdownReader
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseYamlFrontMatter()
        .Build();

    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
            .Build();

    private static readonly Regex WikiLinkPattern = new(@"\[\[([^\]]+)\]\]", RegexOptions.Compiled);

    public static (Dictionary<string, object>? frontmatter, string content, string checksum) ReadMarkdown(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}");

        var fullText = File.ReadAllText(path);
        var checksum = MarkdownWriter.GenerateChecksum(fullText);


        var document = Markdown.Parse(fullText, Pipeline);
        var yamlBlock = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();

        Dictionary<string, object>? frontmatter = null;
        string content = fullText;

        if (yamlBlock != null)
        {

            var yamlText = fullText.Substring(yamlBlock.Span.Start, yamlBlock.Span.Length);


            yamlText = yamlText.Trim('-', '\r', '\n').Trim();

            if (!string.IsNullOrWhiteSpace(yamlText))
            {
                try
                {
                    frontmatter = YamlDeserializer.Deserialize<Dictionary<string, object>>(yamlText);


                    // Do not parse embedding-related fields from frontmatter; embeddings are database-only.
                }
                catch
                {

                }
            }


            var contentStart = yamlBlock.Span.End + 1;
            if (contentStart < fullText.Length)
            {
                content = fullText.Substring(contentStart).TrimStart('\r', '\n');
            }
            else
            {
                content = string.Empty;
            }
        }

        return (frontmatter, content, checksum);
    }

    public static (Dictionary<string, object>? frontmatter, string content, string checksum)
            ReadSession(string thinkingType, string sessionId)
    {
        var path = MarkdownWriter.GetSessionPath(thinkingType, sessionId);
        return ReadMarkdown(path);
    }

    public static List<(string heading, string content)> ExtractH2Sections(string markdown)
    {
        var document = Markdown.Parse(markdown, Pipeline);
        var sections = new List<(string heading, string content)>();
        var blocks = document.ToList();

        for (int i = 0; i < blocks.Count; i++)
        {
            if (blocks[i] is HeadingBlock heading && heading.Level == 2)
            {

                var headingText = "";
                if (heading.Inline != null)
                {
                    var inline = heading.Inline.FirstChild;
                    while (inline != null)
                    {
                        headingText += inline.ToString();
                        inline = inline.NextSibling;
                    }
                }
                headingText = headingText.Trim();

                var content = new StringBuilder();


                for (int j = i + 1; j < blocks.Count; j++)
                {
                    if (blocks[j] is HeadingBlock nextHeading && nextHeading.Level == 2)
                        break;


                    var blockText = GetBlockText(markdown, blocks[j]);
                    if (!string.IsNullOrEmpty(blockText))
                        content.AppendLine(blockText);
                }

                sections.Add((headingText.Trim(), content.ToString().Trim()));
            }
        }

        return sections;
    }

    public static List<string> ExtractWikiLinks(string content)
    {
        return WikiLinkPattern.Matches(content)
            .Cast<Match>()
            .Select(m => MarkdownWriter.NormalizeConcept(m.Groups[1].Value))
            .Distinct()
            .ToList();
    }

    public static int CountConceptOccurrences(string content, string concept)
    {


        var normalizedConcept = MarkdownWriter.NormalizeConcept(concept);
        return WikiLinkPattern.Matches(content)
            .Cast<Match>()
            .Count(m => MarkdownWriter.NormalizeConcept(m.Groups[1].Value) == normalizedConcept);
    }

    private static string GetBlockText(string markdown, Block block)
    {

        if (block.Span.Start >= 0 && block.Span.End <= markdown.Length)
        {
            return markdown.Substring(block.Span.Start, block.Span.Length);
        }
        return "";
    }

    // Embedding-related fields in frontmatter are ignored.
}
