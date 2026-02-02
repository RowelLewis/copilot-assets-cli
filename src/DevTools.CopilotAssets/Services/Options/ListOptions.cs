using DevTools.CopilotAssets.Domain;

namespace DevTools.CopilotAssets.Services;

/// <summary>
/// Options for the list command.
/// </summary>
public sealed record ListOptions
{
    /// <summary>
    /// Target directory (defaults to current directory).
    /// </summary>
    public string TargetDirectory { get; init; } = ".";

    /// <summary>
    /// Filter by asset type.
    /// </summary>
    public AssetTypeFilter? Filter { get; init; }
}
