using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Maenifold.Tools;

public partial class WorkflowTools
{
    [McpServerTool(Title = "Execute Workflow"), Description(@"Orchestrates systematic problem-solving through predefined methodologies with embedded sequential thinking and tool coordination.
Select when AI needs structured approaches, methodology frameworks, multi-step processes, or coordinated tool usage.
Requires workflow selection, session management, response integration, and systematic progression through defined steps.
Connects to SequentialThinking for embedded analysis, all Ma Core tools for orchestrated execution.
Returns structured guidance with step progression, tool hints, quality gates, and systematic methodology execution.")]
    public static string Workflow(
        [Description("Session ID (continue existing)")] string? sessionId = null,
        [Description("Workflow ID(s) to start (single or array)")] string? workflowId = null,
        [Description("Response to current step - MUST include [[concepts]] to build knowledge")] string? response = null,
        [Description("Ambient/meta thoughts - include [[concepts]] (use liberally)")] string? thoughts = null,
        [Description("Set to 'completed' or 'cancelled' to end session")] string? status = null,
        [Description("Required conclusion/synthesis when status='completed' - MUST include [[concepts]]")] string? conclusion = null,
        [Description("View queue status")] bool view = false,
        [Description("Append workflow(s) to queue")] string? append = null)
    {
        ValidateParameters(workflowId, sessionId);
        return DispatchWorkflowOperation(sessionId, workflowId, response, thoughts, status, conclusion, view, append);
    }

    private static void ValidateParameters(string? workflowId, string? sessionId)
    {
        if (workflowId != null && sessionId != null)
        {
            throw new InvalidOperationException("Cannot provide both workflowId and sessionId together.\n\nCURRENT PARAMETERS:\n" +
                   $"  sessionId: {sessionId}\n" +
                   $"  workflowId: {workflowId}\n\n" +
                   "CORRECT USAGE:\n" +
                   "  To continue existing session: Use sessionId + response\n" +
                   "  To start new workflow: Use workflowId only\n" +
                   "  To view queue: Use sessionId + view\n" +
                   "  To append to queue: Use sessionId + append\n\n" +
                   "TIP: You don't need to specify the workflow ID when continuing a session - the session already knows which workflow it's running.");
        }
    }

    private static string DispatchWorkflowOperation(
        string? sessionId,
        string? workflowId,
        string? response,
        string? thoughts,
        string? status,
        string? conclusion,
        bool view,
        string? append)
    {
        // Start new workflow
        if (workflowId != null && sessionId == null)
        {
            return WorkflowOperations.Start(workflowId);
        }

        // View existing session queue
        if (sessionId != null && view)
        {
            return WorkflowOperations.View(sessionId);
        }

        // Append workflow to queue
        if (sessionId != null && append != null)
        {
            return WorkflowOperations.Append(sessionId, append);
        }

        // Continue existing session
        if (sessionId != null && response != null)
        {
            return WorkflowOperations.Continue(sessionId, response, thoughts, status, conclusion);
        }

        throw new InvalidOperationException("Must provide either workflowId (new) or sessionId+response (continue)");
    }
}
