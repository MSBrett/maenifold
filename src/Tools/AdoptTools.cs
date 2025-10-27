using System.ComponentModel;
using ModelContextProtocol.Server;
using Maenifold.Utils;

namespace Maenifold.Tools;

[McpServerToolType]
public class AdoptTools
{
    private static string AssetsPath => Config.AssetsPath;

    [McpServerTool]
    [Description("Adopt a role, color, or perspective by reading its JSON configuration from assets")]
    public static async Task<string> Adopt(
        [Description("Type of asset to adopt: 'role', 'color', or 'perspective'")] string type,
        [Description("Identifier of the asset (filename without .json extension)")] string identifier
    )
    {

        var validTypes = new[] { "role", "color", "perspective" };
        if (!validTypes.Contains(type.ToLowerInvariant(), StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Invalid type '{type}'. Must be one of: {string.Join(", ", validTypes)}");
        }


        var folderName = type.ToLowerInvariant() + "s";


        var filePath = Path.Combine(AssetsPath, folderName, $"{identifier}.json");


        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Asset not found: {type}/{identifier}");
        }


        return await File.ReadAllTextAsync(filePath);
    }
}