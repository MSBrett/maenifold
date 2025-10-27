using System.Text;
using Maenifold.Utils;

namespace Maenifold.Tools;

public static class RecentActivityFormatter
{
    public static string FormatActivityReport(
    List<(string filePath, string title, string lastIndexed, string content, string status)> results,
    bool includeContent)
    {
        if (results.Count == 0)
        {
            return "No recent activity found";
        }

        var output = new StringBuilder();
        output.AppendLine("# Recent Activity");
        output.AppendLine();

        foreach (var (filePath, title, lastIndexed, content, status) in results)
        {
            var sessionId = RecentActivityReader.ExtractSessionId(filePath);
            var type = RecentActivityReader.DetermineFileType(filePath);
            var modified = CultureInvariantHelpers.ParseDateTime(lastIndexed);


            var sections = MarkdownIO.ExtractH2Sections(content);


            if (type == "sequential" || type == "workflow")
            {
                ProcessThinkingFile(output, sessionId, type, modified, sections, content, status, includeContent);
            }
            else
            {
                ProcessMemoryFile(output, sessionId, type, modified, sections, title, includeContent);
            }

            output.AppendLine();
        }

        return output.ToString();
    }

    private static void ProcessThinkingFile(StringBuilder output, string sessionId, string type, DateTime modified,
            List<(string heading, string content)> sections, string fullContent, string status, bool includeContent)
    {
        if (type == "sequential" || sections.Any(s => s.heading.StartsWithOrdinal("Thought")))
        {

            var thoughtCount = sections.Count(s => s.heading.StartsWithOrdinal("Thought"));


            var meaningfulSections = sections
                            .Where(s => !s.heading.Equals("Completion", StringComparison.Ordinal) &&
                                        !s.heading.Equals("Cancellation", StringComparison.Ordinal))
                            .ToList();

            output.AppendLineInvariant($"**{sessionId}** (sequential)");
            output.AppendLineInvariant($"  Modified: {CultureInvariantHelpers.FormatDateTime(modified, "yyyy-MM-dd HH:mm")}");
            output.AppendLineInvariant($"  Thoughts: {thoughtCount}");
            output.AppendLineInvariant($"  Status: {status}");


            if (meaningfulSections.Count > 0)
            {
                if (includeContent)
                {
                    output.AppendLineInvariant($"  First H2: {meaningfulSections[0].heading}");
                    output.AppendLineInvariant($"  First H2 Content: {meaningfulSections[0].content}");
                }
                else
                {
                    var firstContent = RecentActivityReader.ExtractSnippet(meaningfulSections[0].content);
                    output.AppendLineInvariant($"  First: \"{firstContent}\"");
                }
            }


            if (meaningfulSections.Count > 1)
            {
                if (includeContent)
                {
                    output.AppendLineInvariant($"  Last H2: {meaningfulSections.Last().heading}");
                    output.AppendLineInvariant($"  Last H2 Content: {meaningfulSections.Last().content}");
                }
                else
                {
                    var lastContent = RecentActivityReader.ExtractSnippet(meaningfulSections.Last().content);
                    if (lastContent != RecentActivityReader.ExtractSnippet(meaningfulSections[0].content))
                    {
                        output.AppendLineInvariant($"  Last: \"{lastContent}\"");
                    }
                }
            }
        }
        else
        {

            var stepCount = sections.Count(s =>
                            s.heading.ContainsOrdinal("Step") ||
                            s.heading.ContainsOrdinal("Response"));


            var meaningfulSections = sections
                            .Where(s => !s.heading.Equals("Completion", StringComparison.Ordinal) &&
                                        !s.heading.Equals("Cancellation", StringComparison.Ordinal))
                            .ToList();

            output.AppendLineInvariant($"**{sessionId}** (workflow)");
            output.AppendLineInvariant($"  Modified: {CultureInvariantHelpers.FormatDateTime(modified, "yyyy-MM-dd HH:mm")}");
            output.AppendLineInvariant($"  Steps: {stepCount}");
            output.AppendLineInvariant($"  Status: {status}");


            if (meaningfulSections.Count > 0)
            {
                if (includeContent)
                {
                    output.AppendLineInvariant($"  First H2: {meaningfulSections[0].heading}");
                    output.AppendLineInvariant($"  First H2 Content: {meaningfulSections[0].content}");
                }
                else
                {
                    var firstContent = RecentActivityReader.ExtractSnippet(meaningfulSections[0].content);
                    output.AppendLineInvariant($"  First: \"{firstContent}\"");
                }
            }


            if (meaningfulSections.Count > 1)
            {
                if (includeContent)
                {
                    output.AppendLineInvariant($"  Last H2: {meaningfulSections.Last().heading}");
                    output.AppendLineInvariant($"  Last H2 Content: {meaningfulSections.Last().content}");
                }
                else
                {
                    var currentContent = RecentActivityReader.ExtractSnippet(meaningfulSections.Last().content);
                    if (currentContent != RecentActivityReader.ExtractSnippet(meaningfulSections[0].content))
                    {
                        output.AppendLineInvariant($"  Current: \"{currentContent}\"");
                    }
                }
            }
        }
    }

    private static void ProcessMemoryFile(StringBuilder output, string sessionId, string type, DateTime modified,
            List<(string heading, string content)> sections, string title, bool includeContent)
    {
        output.AppendLineInvariant($"**{sessionId}** (memory)");
        output.AppendLineInvariant($"  Modified: {CultureInvariantHelpers.FormatDateTime(modified, "yyyy-MM-dd HH:mm")}");
        output.AppendLineInvariant($"  Title: {title}");
        output.AppendLineInvariant($"  Sections: {sections.Count}");


        if (sections.Count > 0)
        {
            if (includeContent)
            {
                output.AppendLineInvariant($"  First H2: {sections[0].heading}");
                output.AppendLineInvariant($"  First H2 Content: {sections[0].content}");
            }
            else
            {
                var firstContent = RecentActivityReader.ExtractSnippet(sections[0].content);
                output.AppendLineInvariant($"  First: \"{firstContent}\"");
            }
        }


        if (sections.Count > 1)
        {
            if (includeContent)
            {
                output.AppendLineInvariant($"  Last H2: {sections.Last().heading}");
                output.AppendLineInvariant($"  Last H2 Content: {sections.Last().content}");
            }
            else
            {
                var lastContent = RecentActivityReader.ExtractSnippet(sections.Last().content);
                if (lastContent != RecentActivityReader.ExtractSnippet(sections[0].content))
                {
                    output.AppendLineInvariant($"  Last: \"{lastContent}\"");
                }
            }
        }
    }
}
