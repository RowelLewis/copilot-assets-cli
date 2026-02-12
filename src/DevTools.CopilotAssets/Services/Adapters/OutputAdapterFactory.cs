using DevTools.CopilotAssets.Domain;

namespace DevTools.CopilotAssets.Services.Adapters;

/// <summary>
/// Creates output adapters for specified target tools.
/// </summary>
public sealed class OutputAdapterFactory
{
    private static readonly Dictionary<TargetTool, Func<IOutputAdapter>> AdapterCreators = new()
    {
        [TargetTool.Copilot] = () => new CopilotOutputAdapter(),
        [TargetTool.Claude] = () => new ClaudeOutputAdapter(),
        [TargetTool.Cursor] = () => new CursorOutputAdapter(),
        [TargetTool.Windsurf] = () => new WindsurfOutputAdapter(),
        [TargetTool.Cline] = () => new ClineOutputAdapter(),
        [TargetTool.Aider] = () => new AiderOutputAdapter()
    };

    /// <summary>
    /// Create adapters for the specified target tools.
    /// </summary>
    public IReadOnlyList<IOutputAdapter> CreateAdapters(IEnumerable<TargetTool> targets)
    {
        return targets
            .Distinct()
            .Select(t => AdapterCreators[t]())
            .ToList();
    }

    /// <summary>
    /// Create a single adapter for the specified target tool.
    /// </summary>
    public IOutputAdapter CreateAdapter(TargetTool target)
    {
        return AdapterCreators[target]();
    }

    /// <summary>
    /// Get all available target tools.
    /// </summary>
    public static IReadOnlyList<TargetTool> AvailableTargets => AdapterCreators.Keys.ToList();
}
