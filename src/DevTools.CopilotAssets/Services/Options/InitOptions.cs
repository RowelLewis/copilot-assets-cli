using DevTools.CopilotAssets.Domain;

namespace DevTools.CopilotAssets.Services;

/// <summary>
/// Options for the init command.
/// </summary>
public sealed record InitOptions
{
    /// <summary>
    /// Target directory (defaults to current directory).
    /// </summary>
    public string TargetDirectory { get; init; } = ".";

    /// <summary>
    /// Overwrite existing files without prompting.
    /// </summary>
    public bool Force { get; init; }

    /// <summary>
    /// Skip git operations (stage/commit).
    /// </summary>
    public bool NoGit { get; init; }

    /// <summary>
    /// Filter to apply when selecting assets.
    /// </summary>
    public AssetTypeFilter? Filter { get; init; }
}
