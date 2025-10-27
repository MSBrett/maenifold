using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Maenifold.Tools;

[McpServerToolType]
public partial class WorkflowTools
{
    [McpServerTool, Description(@"Displays available workflow types for systematic problem-solving and orchestrated thinking processes.
Select when AI needs structured approaches to complex tasks, methodology guidance, or process frameworks.
No parameters required - returns complete workflow catalog with descriptions and use cases.
Connects to Workflow tool for execution, SequentialThinking for coordination, all Ma Core tools for orchestration.
Returns workflow list with names, descriptions, and applicability guidance for systematic task approaches.")]
    public static string ListWorkflows()
    {
        return WorkflowListTools.ListWorkflows();
    }
}
