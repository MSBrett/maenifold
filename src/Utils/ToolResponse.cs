namespace Maenifold.Utils;

public static class ToolResponse
{
    public static string WithHint(string result, string toolName, string hint)
    {
        return $"{result}\n💡 {hint} Run GetHelp(\"{toolName}\") for complete documentation.";
    }

    public static string WithNextSteps(string result, params string[] nextTools)
    {
        var steps = string.Join(", ", nextTools);
        return $"{result}\n💡 Next: Consider using {steps} for related operations.";
    }

    public static string WithTip(string result, string tip)
    {
        return $"{result}\n💡 Tip: {tip}";
    }
}