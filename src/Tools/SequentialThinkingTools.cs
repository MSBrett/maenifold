using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using Maenifold.Utils;

namespace Maenifold.Tools;

[McpServerToolType]
public class SequentialThinkingTools
{
    private static readonly int CheckpointFrequency = 3;

    [McpServerTool, Description(@"Creates structured thinking sessions with [[concept]] integration and persistent markdown file storage.
Requires response with [[concepts]], thought tracking, session management, and optional revision capabilities.
Integrates with WriteMemory for session persistence and Ma Core tools.
Returns session management with continuation guidance and checkpoint suggestions.")]
    public static string SequentialThinking(
        [Description("Main response/thought - MUST include [[concepts]] to build knowledge")] string? response = null,
        [Description("Need another thought?")] bool nextThoughtNeeded = false,
        [Description("Current thought number")] int thoughtNumber = 0,
        [Description("Total thoughts estimate")] int totalThoughts = 0,
        [Description("Session ID")] string? sessionId = null,
        [Description("Cancel session (set to true to cancel)")] bool cancel = false,
        [Description("Ambient/meta thoughts with [[concepts]] (use liberally)")] string? thoughts = null,
        [Description("Is this a revision?")] bool isRevision = false,
        [Description("Which thought to revise")] int? revisesThought = null,
        [Description("Branch from thought")] int? branchFromThought = null,
        [Description("Branch ID")] string? branchId = null,
        [Description("Need more thoughts than estimated?")] bool needsMoreThoughts = false,
        [Description("Analysis type: bug, architecture, retrospective, or complex")] string? analysisType = null,
        [Description("Parent workflow session ID (creates bidirectional link)")] string? parentWorkflowId = null,
        [Description("Required conclusion/synthesis when nextThoughtNeeded=false - MUST include [[concepts]]")] string? conclusion = null)
    {
        // Skip validation if cancel operation
        if (!cancel)
        {
            var (isValid, validationError) = ValidateThinkingInput(response, thoughts);
            if (!isValid)
                return validationError!;
        }

        var (sessionExists, sessionIdProvided) = DetermineSessionState(sessionId, thoughtNumber, isRevision);

        if (sessionId == null)
            sessionId = $"session-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        if (sessionIdProvided && thoughtNumber == 1 && !sessionExists && !isRevision)
            return $"ERROR: Session {sessionId} not found. To start new session, don't provide sessionId.";

        var parentWorkflowError = ValidateParentWorkflow(parentWorkflowId, thoughtNumber);
        if (parentWorkflowError != null)
            return parentWorkflowError;

        // Validate branchId requirement for multi-agent safety
        if (branchFromThought.HasValue && string.IsNullOrEmpty(branchId))
            return "ERROR: branchId required when branchFromThought is specified for multi-agent coordination";

        if (thoughtNumber == 1 && string.IsNullOrEmpty(branchId) && sessionExists && !isRevision)
            return $"ERROR: Session {sessionId} exists. Use different sessionId or continue existing.";
        if (thoughtNumber > 1 && !sessionExists && !isRevision)
            return $"ERROR: Session {sessionId} missing. Start with thoughtNumber=1.";
        if (isRevision && !sessionExists)
            return $"ERROR: Cannot revise - session {sessionId} doesn't exist.";

        if (!sessionExists)
        {
            CreateNewSession(sessionId, analysisType, parentWorkflowId);
            if (!string.IsNullOrEmpty(parentWorkflowId))
            {
                LinkParentWorkflow(sessionId, parentWorkflowId);
            }
        }

        // Only append thought section if not cancelling
        if (!cancel && response != null)
        {
            var (heading, contentBuilder) = BuildThoughtSection(thoughtNumber, totalThoughts, needsMoreThoughts, branchId, isRevision, revisesThought, response, thoughts);
            MarkdownIO.AppendToSession("sequential", sessionId, heading, contentBuilder);
        }

        var complete = !nextThoughtNeeded;
        if (complete && cancel == false)
        {
            if (string.IsNullOrEmpty(conclusion))
                return "ERROR: Conclusion required when completing session. Must synthesize findings with [[concepts]].";

            var conclusionConcepts = MarkdownIO.ExtractWikiLinks(conclusion);
            if (conclusionConcepts.Count == 0)
                return "ERROR: Conclusion must include [[concepts]] for knowledge graph integration.";
        }

        FinalizeSession(sessionId, thoughtNumber, cancel, complete, conclusion);

        var responseMessage = BuildCompletionMessage(thoughtNumber, sessionId, cancel, nextThoughtNeeded, needsMoreThoughts, totalThoughts);
        return responseMessage;
    }

    private static (bool isValid, string? errorMessage) ValidateThinkingInput(string? response, string? thoughts)
    {
        var responseConcepts = string.IsNullOrEmpty(response) ? new List<string>() : MarkdownIO.ExtractWikiLinks(response);
        var thoughtsConcepts = string.IsNullOrEmpty(thoughts) ? new List<string>() : MarkdownIO.ExtractWikiLinks(thoughts);
        var totalConcepts = responseConcepts.Count + thoughtsConcepts.Count;

        if (totalConcepts == 0)
            return (false, "ERROR: Must include [[concepts]]. Example: 'Analyzing [[Machine Learning]] algorithms'");

        return (true, null);
    }

    private static (bool sessionExists, bool sessionIdProvided) DetermineSessionState(string? sessionId, int thoughtNumber, bool isRevision)
    {
        bool sessionIdWasProvided = sessionId != null;
        bool sessionExists = sessionIdWasProvided && sessionId != null && MarkdownIO.SessionExists("sequential", sessionId);

        return (sessionExists, sessionIdWasProvided);
    }

    private static string? ValidateParentWorkflow(string? parentWorkflowId, int thoughtNumber)
    {
        if (string.IsNullOrEmpty(parentWorkflowId))
            return null;

        if (thoughtNumber != 1)
            return "ERROR: Parent workflow can only be set on first thought.";

        if (!MarkdownIO.SessionExists("workflow", parentWorkflowId))
            return $"ERROR: Parent workflow '{parentWorkflowId}' not found.";

        var (parentMeta, _, _) = MarkdownIO.ReadSession("workflow", parentWorkflowId);
        var parentStatus = parentMeta?.ContainsKey("status") == true ? parentMeta["status"]?.ToString() : "active";

        if (parentStatus == "completed" || parentStatus == "cancelled" || parentStatus == "abandoned")
            return $"ERROR: Parent workflow is {parentStatus}.";

        return null;
    }

    private static (string heading, string content) BuildThoughtSection(int thoughtNumber, int totalThoughts, bool needsMoreThoughts, string? branchId, bool isRevision, int? revisesThought, string response, string? thoughts)
    {
        var displayTotal = totalThoughts;
        if (needsMoreThoughts && thoughtNumber >= totalThoughts)
        {
            displayTotal = thoughtNumber + 1;
        }

        var agentId = Environment.GetEnvironmentVariable("AGENT_ID") ?? "agent";
        var heading = new StringBuilder();
        heading.AppendInvariant($"Thought {thoughtNumber}/{displayTotal} [{agentId}]");

        if (!string.IsNullOrEmpty(branchId))
            heading.AppendInvariant($" (Branch: {branchId})");
        else if (isRevision && revisesThought.HasValue)
            heading.AppendInvariant($" (Revises: {revisesThought})");

        var content = new StringBuilder();
        content.AppendLine(response);
        if (!string.IsNullOrEmpty(thoughts))
        {
            content.AppendLine();
            content.AppendLine(CultureInfo.InvariantCulture, $"*Thoughts: {thoughts}*");
        }
        content.AppendLine();
        content.AppendLine(CultureInfo.InvariantCulture, $"*{CultureInvariantHelpers.FormatDateTime(DateTime.UtcNow, "yyyy-MM-dd HH:mm:ss")}*");

        return (heading.ToString(), content.ToString());
    }

    private static void CreateNewSession(string sessionId, string? analysisType, string? parentWorkflowId)
    {
        var frontmatter = new Dictionary<string, object>
        {
            ["title"] = $"Sequential Thinking Session {sessionId}",
            ["permalink"] = sessionId,
            ["type"] = "sequential",
            ["status"] = "active"
        };

        if (!string.IsNullOrEmpty(analysisType))
        {
            frontmatter["analysisType"] = analysisType;
        }

        if (!string.IsNullOrEmpty(parentWorkflowId))
        {
            frontmatter["parent"] = $"[[workflow/{parentWorkflowId}]]";
        }

        var initialContent = "# Sequential Thinking Session\n\n";

        MarkdownIO.CreateSession("sequential", sessionId, frontmatter, initialContent);
    }

    private static void LinkParentWorkflow(string sessionId, string parentWorkflowId)
    {
        var (parentMeta, parentContent, _) = MarkdownIO.ReadSession("workflow", parentWorkflowId);
        if (parentMeta == null) parentMeta = new Dictionary<string, object>();

        var related = parentMeta.TryGetValue("related", out var relatedValue) ?
            (relatedValue as List<object>)?.Cast<string>().ToList() ?? new List<string>() :
            new List<string>();

        related.Add($"[[sequential/{sessionId}]]");
        parentMeta["related"] = related;

        MarkdownIO.UpdateSession("workflow", parentWorkflowId, parentMeta, parentContent);
    }

    private static void FinalizeSession(string sessionId, int thoughtNumber, bool cancel, bool complete, string? conclusion)
    {
        if (cancel)
        {
            var (frontmatter, existingContent, _) = MarkdownIO.ReadSession("sequential", sessionId);
            frontmatter ??= new Dictionary<string, object>();
            frontmatter["status"] = "cancelled";
            frontmatter["cancelled"] = DateTime.UtcNow.ToString("o");
            // Use thoughtNumber if provided (non-zero), otherwise preserve existing thoughtCount
            if (thoughtNumber > 0)
            {
                frontmatter["thoughtCount"] = thoughtNumber;
            }
            else if (!frontmatter.ContainsKey("thoughtCount"))
            {
                // If no thoughtNumber provided and no existing thoughtCount, use 0
                frontmatter["thoughtCount"] = 0;
            }
            MarkdownIO.UpdateSession("sequential", sessionId, frontmatter, existingContent);
        }
        else if (complete && conclusion != null)
        {
            MarkdownIO.AppendToSession("sequential", sessionId, "Conclusion", conclusion);

            var (frontmatter, existingContent, _) = MarkdownIO.ReadSession("sequential", sessionId);
            frontmatter ??= new Dictionary<string, object>();
            frontmatter["status"] = "completed";
            frontmatter["completed"] = DateTime.UtcNow.ToString("o");
            frontmatter["thoughtCount"] = thoughtNumber;
            MarkdownIO.UpdateSession("sequential", sessionId, frontmatter, existingContent);
        }
    }

    private static string BuildCompletionMessage(int thoughtNumber, string sessionId, bool cancel, bool nextThoughtNeeded, bool needsMoreThoughts, int totalThoughts)
    {
        var responseMessage = thoughtNumber == 1
            ? $"Created session: {sessionId}"
            : $"Added thought {thoughtNumber} to session: {sessionId}";

        if (cancel)
        {
            responseMessage += "\n\n❌ Thinking cancelled";
        }
        else if (!nextThoughtNeeded)
        {
            responseMessage += "\n\n✅ Thinking complete";
        }
        else
        {
            var nextThought = thoughtNumber + 1;
            var nextTotal = needsMoreThoughts && thoughtNumber >= totalThoughts ? "?" : CultureInvariantHelpers.ToString(totalThoughts);
            responseMessage += $"\n\n💭 Continue with thought {nextThought}/{nextTotal}";
            if (needsMoreThoughts && thoughtNumber >= totalThoughts)
            {
                responseMessage += " (extending beyond initial estimate)";
            }

            if (thoughtNumber == 1 || thoughtNumber % CheckpointFrequency == 0)
            {
                responseMessage += "\n\n💡 **CHECK YOUR MEMORY:** `search_memories` for what exists and `build_context` on [[concepts]] | `sync` new findings to add them to the graph";
            }
        }

        return responseMessage;
    }

}
