using DevTools.CopilotAssets.Domain;

namespace DevTools.CopilotAssets.Services;

/// <summary>
/// Result of diagnostics check.
/// </summary>
public sealed class DiagnosticsResult
{
    public bool GitAvailable { get; init; }
    public bool IsGitRepository { get; init; }
    public bool AssetsDirectoryExists { get; init; }
    public bool ManifestExists { get; init; }
    public TemplateSource? Source { get; init; }
    public string ToolVersion { get; init; } = "1.0.0";
    public List<string> Issues { get; } = [];

    public bool HasIssues => Issues.Count > 0;
}
