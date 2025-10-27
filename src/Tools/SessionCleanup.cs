using Maenifold.Utils;

namespace Maenifold.Tools;

public static class SessionCleanup
{
    public static void HandleSessionCleanup(Dictionary<string, object?> frontmatter, string filePath, string content)
    {
        var fileType = frontmatter.TryGetValue("type", out var typeValue) ? typeValue?.ToString() : null;
        var status = frontmatter.TryGetValue("status", out var statusValue) ? statusValue?.ToString() : null;

        if ((fileType == "workflow" || fileType == "sequential") && status == "active")
        {
            var modified = frontmatter.TryGetValue("modified", out var modifiedValue) ? modifiedValue?.ToString() : null;
            if (DateTime.TryParse(modified, out var lastModifiedTime))
            {
                var timeSinceUpdate = DateTime.UtcNow - lastModifiedTime.ToUniversalTime();
                if (timeSinceUpdate.TotalMinutes > Config.SessionAbandonmentMinutes)
                {

                    frontmatter["status"] = "abandoned";
                    MarkdownIO.WriteMarkdown(filePath, frontmatter!, content);
                    MarkdownIO.AppendH2Section(filePath, "Session Abandoned",
                        $"⚠️ Session marked as abandoned due to {timeSinceUpdate.TotalMinutes:F0} minutes of inactivity\n*{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}*");
                }
            }
        }
    }
}