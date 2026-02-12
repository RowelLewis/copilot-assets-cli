namespace DevTools.CopilotAssets.Domain;

/// <summary>
/// Target AI coding tools for asset generation.
/// </summary>
public enum TargetTool
{
    Copilot,
    Claude,
    Cursor,
    Windsurf,
    Cline,
    Aider
}

/// <summary>
/// Extension methods for TargetTool.
/// </summary>
public static class TargetToolExtensions
{
    private static readonly Dictionary<string, TargetTool> NameMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["copilot"] = TargetTool.Copilot,
        ["claude"] = TargetTool.Claude,
        ["cursor"] = TargetTool.Cursor,
        ["windsurf"] = TargetTool.Windsurf,
        ["cline"] = TargetTool.Cline,
        ["aider"] = TargetTool.Aider
    };

    /// <summary>
    /// Parse a comma-separated list of target tool names.
    /// </summary>
    public static (bool Success, List<TargetTool>? Tools, string? Error) ParseTargets(string value)
    {
        var tools = new List<TargetTool>();
        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            if (!NameMap.TryGetValue(part, out var tool))
            {
                var validNames = string.Join(", ", NameMap.Keys);
                return (false, null, $"Unknown target tool: '{part}'. Valid targets: {validNames}");
            }
            if (!tools.Contains(tool))
            {
                tools.Add(tool);
            }
        }

        if (tools.Count == 0)
            return (false, null, "No valid target tools specified");

        return (true, tools, null);
    }

    /// <summary>
    /// Get the lowercase string name of a target tool.
    /// </summary>
    public static string ToConfigName(this TargetTool tool) => tool.ToString().ToLowerInvariant();
}
