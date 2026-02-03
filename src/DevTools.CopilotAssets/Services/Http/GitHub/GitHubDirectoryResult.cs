namespace DevTools.CopilotAssets.Services.Http;

/// <summary>
/// Result of fetching a directory from GitHub.
/// </summary>
public sealed class GitHubDirectoryResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public List<GitHubFileInfo> Files { get; init; } = [];
}
