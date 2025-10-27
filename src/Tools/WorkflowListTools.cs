using System.Text.Json;

namespace Maenifold.Tools;

public static class WorkflowListTools
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string ListWorkflows()
    {
        try
        {
            if (!Directory.Exists(WorkflowCommon.WorkflowsPath))
            {
                return "ERROR: Workflows directory not found";
            }

            var files = Directory.GetFiles(WorkflowCommon.WorkflowsPath, "*.json");
            var workflows = new List<object>();

            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var workflow = JsonSerializer.Deserialize<JsonElement>(json);

                    var id = workflow.TryGetProperty("id", out var idProp) ? idProp.GetString() : Path.GetFileNameWithoutExtension(file);
                    var name = workflow.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : "Unknown Workflow";
                    var emoji = workflow.TryGetProperty("emoji", out var emojiProp) ? emojiProp.GetString() : "❓";
                    var description = workflow.TryGetProperty("shortDescription", out var descProp) ? descProp.GetString() : "No description available";

                    workflows.Add(new
                    {
                        id = id,
                        name = name,
                        emoji = emoji,
                        description = description
                    });
                }
                catch (JsonException)
                {
                }
                catch (Exception)
                {
                }
            }

            return JsonSerializer.Serialize(workflows, JsonOptions);
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }
}
