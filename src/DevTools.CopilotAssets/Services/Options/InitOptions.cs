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

    /// <summary>
    /// Template source override (e.g., "owner/repo[@branch]").
    /// When set, overrides the configured remote source.
    /// Null means use configured source or default.
    /// </summary>
    public string? SourceOverride { get; init; }

    /// <summary>
    /// When true, explicitly use default (bundled) templates
    /// even if a remote source is configured.
    /// </summary>
    public bool UseDefaultTemplates { get; init; }

    /// <summary>
    /// Target AI tools to generate output for.
    /// Defaults to Copilot only for backward compatibility.
    /// </summary>
    public IReadOnlyList<TargetTool>? Targets { get; init; }
}
