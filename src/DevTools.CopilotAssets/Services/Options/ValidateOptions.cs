namespace DevTools.CopilotAssets.Services;

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
