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

    /// <summary>
    /// List all installed assets.
    /// </summary>
    Task<AssetListResult> ListAssetsAsync(ListOptions options);

    /// <summary>
    /// Verify file integrity against manifest checksums.
    /// </summary>
    Task<VerifyResult> VerifyAsync(VerifyOptions options);

    /// <summary>
    /// Preview what changes would be made without modifying anything.
    /// </summary>
    Task<DryRunResult> PreviewInitAsync(InitOptions options);

    /// <summary>
    /// Preview what update would do without modifying anything.
    /// </summary>
    Task<DryRunResult> PreviewUpdateAsync(UpdateOptions options);

    /// <summary>
    /// Get pending file operations for interactive mode.
    /// </summary>
    Task<(List<PendingFile> Files, string? Source, string? Error)> GetPendingOperationsAsync(
        string targetDirectory,
        AssetTypeFilter? filter = null,
        string? sourceOverride = null,
        CancellationToken ct = default);

    /// <summary>
    /// Execute sync for selected files only (interactive mode).
    /// </summary>
    ValidationResult ExecuteSelectiveSync(
        string targetDirectory,
        IEnumerable<PendingFile> selectedFiles,
        string? source = null,
        bool noGit = false);
}
