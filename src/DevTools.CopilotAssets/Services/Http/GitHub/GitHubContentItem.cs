namespace DevTools.CopilotAssets.Services.Http;

/// <summary>
/// GitHub API content item.
/// </summary>
internal sealed class GitHubContentItem
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public string Type { get; set; } = "";
    public string? Url { get; set; }
    public string? DownloadUrl { get; set; }
}
