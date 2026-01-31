using DevTools.CopilotAssets.Domain;

namespace DevTools.CopilotAssets.Services;

/// <summary>
/// Options for the update command.
/// </summary>
public sealed record UpdateOptions
{
    /// <summary>
    /// Target directory (defaults to current directory).
    /// </summary>
    public string TargetDirectory { get; init; } = ".";

    /// <summary>
    /// Overwrite local changes without prompting.
    /// </summary>
    public bool Force { get; init; }

    /// <summary>
    /// Skip git operations.
    /// </summary>
    public bool NoGit { get; init; }

    /// <summary>
    /// Filter to apply when selecting assets.
    /// </summary>
    public AssetTypeFilter? Filter { get; init; }
}
