using DevTools.CopilotAssets.Domain;

namespace DevTools.CopilotAssets.Services;

/// <summary>
/// Core application service interface for policy operations.
/// </summary>
public interface IPolicyAppService
{
    /// <summary>
    /// Initialize a project with Copilot assets.
    /// </summary>
    Task<ValidationResult> InitAsync(InitOptions options);

    /// <summary>
    /// Update existing assets to latest version.
    /// </summary>
    Task<ValidationResult> UpdateAsync(UpdateOptions options);

    /// <summary>
    /// Validate project compliance with policy.
    /// </summary>
    Task<ValidationResult> ValidateAsync(ValidateOptions options);

    /// <summary>
    /// Run diagnostics on the environment.
    /// </summary>
    Task<DiagnosticsResult> DiagnoseAsync();
}

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
}

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
}

/// <summary>
/// Options for the validate command.
/// </summary>
public sealed record ValidateOptions
{
    /// <summary>
    /// Target directory (defaults to current directory).
    /// </summary>
    public string TargetDirectory { get; init; } = ".";

    /// <summary>
    /// Running in CI mode (JSON output, strict exit codes).
    /// </summary>
    public bool CiMode { get; init; }
}

/// <summary>
/// Result of diagnostics check.
/// </summary>
public sealed class DiagnosticsResult
{
    public bool GitAvailable { get; init; }
    public bool IsGitRepository { get; init; }
    public bool AssetsDirectoryExists { get; init; }
    public bool ManifestExists { get; init; }
    public string? InstalledVersion { get; init; }
    public string ToolVersion { get; init; } = "1.0.0";
    public List<string> Issues { get; } = [];

    public bool HasIssues => Issues.Count > 0;
}
