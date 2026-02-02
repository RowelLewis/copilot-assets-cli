namespace DevTools.CopilotAssets.Services.Http;

/// <summary>
/// Information about a file in a GitHub repository.
/// </summary>
public sealed class GitHubFileInfo
{
    public required string Path { get; init; }
    public required string DownloadUrl { get; init; }
}
