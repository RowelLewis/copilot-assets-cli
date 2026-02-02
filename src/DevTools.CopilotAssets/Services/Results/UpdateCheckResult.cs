namespace DevTools.CopilotAssets.Services.Results;

/// <summary>
/// Result of checking for updates by comparing checksums.
/// </summary>
public sealed class UpdateCheckResult
{
    /// <summary>
    /// Files that would be added (exist in templates but not installed).
    /// </summary>
    public List<string> Added { get; } = [];

    /// <summary>
    /// Files that have been modified (checksums differ).
    /// </summary>
    public List<string> Modified { get; } = [];

    /// <summary>
    /// Files that would be removed (installed but not in templates).
    /// </summary>
    public List<string> Removed { get; } = [];

    /// <summary>
    /// Files that are unchanged.
    /// </summary>
    public List<string> Unchanged { get; } = [];

    /// <summary>
    /// Whether assets are not installed at all.
    /// </summary>
    public bool NotInstalled { get; set; }

    /// <summary>
    /// Error message if check failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Whether there are any changes to apply.
    /// </summary>
    public bool HasChanges => Added.Count > 0 || Modified.Count > 0 || Removed.Count > 0;

    /// <summary>
    /// Total number of changes.
    /// </summary>
    public int TotalChanges => Added.Count + Modified.Count + Removed.Count;

    /// <summary>
    /// Whether the check was successful.
    /// </summary>
    public bool Success => Error == null && !NotInstalled;
}
