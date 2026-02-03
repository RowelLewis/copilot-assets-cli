using DevTools.CopilotAssets.Domain;

namespace DevTools.CopilotAssets.Services;

/// <summary>
/// Options for the verify command.
/// </summary>
public sealed record VerifyOptions
{
    /// <summary>
    /// Target directory (defaults to current directory).
    /// </summary>
    public string TargetDirectory { get; init; } = ".";

    /// <summary>
    /// Restore modified files to original state.
    /// </summary>
    public bool Restore { get; init; }

    /// <summary>
    /// Filter by asset type.
    /// </summary>
    public AssetTypeFilter? Filter { get; init; }
}
