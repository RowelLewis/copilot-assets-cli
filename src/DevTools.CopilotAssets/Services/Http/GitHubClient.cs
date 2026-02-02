using System.Net.Http.Headers;
using System.Text.Json;

namespace DevTools.CopilotAssets.Services.Http;

/// <summary>
/// HTTP client for fetching files from GitHub repositories.
/// Uses the GitHub API and raw.githubusercontent.com.
/// </summary>
public sealed class GitHubClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private const string GitHubApiBase = "https://api.github.com";
    private const string RawGitHubBase = "https://raw.githubusercontent.com";

    public GitHubClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? CreateDefaultClient();
    }

    private static HttpClient CreateDefaultClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("copilot-assets-cli", SyncEngine.ToolVersion));
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
        client.Timeout = TimeSpan.FromSeconds(30);

        // Add GitHub token for authentication (supports private repos)
        var token = GetGitHubToken();
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return client;
    }

    /// <summary>
    /// Get GitHub token from gh CLI or environment variables.
    /// Priority: gh CLI → GITHUB_TOKEN → GH_TOKEN
    /// </summary>
    private static string? GetGitHubToken()
    {
        // Try gh CLI first (best for local CLI usage)
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "gh",
                    Arguments = "auth token",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
            {
                return output;
            }
        }
        catch
        {
            // gh CLI not available or failed, try env vars
        }

        // Try GITHUB_TOKEN (explicit override or CI)
        var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        if (!string.IsNullOrEmpty(token))
        {
            return token;
        }

        // Try GH_TOKEN (GitHub Actions)
        token = Environment.GetEnvironmentVariable("GH_TOKEN");
        if (!string.IsNullOrEmpty(token))
        {
            return token;
        }

        return null;
    }

    /// <summary>
    /// Get list of files in a directory from a GitHub repository.
    /// </summary>
    /// <param name="owner">Repository owner.</param>
    /// <param name="repo">Repository name.</param>
    /// <param name="path">Path within repository.</param>
    /// <param name="branch">Branch name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of file paths relative to the specified path.</returns>
    public async Task<GitHubDirectoryResult> GetDirectoryContentsAsync(
        string owner,
        string repo,
        string path,
        string branch,
        CancellationToken ct = default)
    {
        var url = $"{GitHubApiBase}/repos/{owner}/{repo}/contents/{path}?ref={branch}";

        try
        {
            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                return new GitHubDirectoryResult
                {
                    Success = false,
                    Error = $"GitHub API returned {response.StatusCode}"
                };
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var items = JsonSerializer.Deserialize<GitHubContentItem[]>(json, JsonOptions);

            if (items == null)
            {
                return new GitHubDirectoryResult
                {
                    Success = false,
                    Error = "Failed to parse GitHub API response"
                };
            }

            var files = new List<GitHubFileInfo>();
            await CollectFilesRecursivelyAsync(owner, repo, branch, items, "", files, ct);

            return new GitHubDirectoryResult
            {
                Success = true,
                Files = files
            };
        }
        catch (HttpRequestException ex)
        {
            return new GitHubDirectoryResult
            {
                Success = false,
                Error = $"Network error: {ex.Message}"
            };
        }
        catch (TaskCanceledException)
        {
            return new GitHubDirectoryResult
            {
                Success = false,
                Error = "Request timed out"
            };
        }
    }

    private async Task CollectFilesRecursivelyAsync(
        string owner,
        string repo,
        string branch,
        GitHubContentItem[] items,
        string currentPath,
        List<GitHubFileInfo> files,
        CancellationToken ct)
    {
        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();

            var relativePath = string.IsNullOrEmpty(currentPath)
                ? item.Name
                : $"{currentPath}/{item.Name}";

            if (item.Type == "file")
            {
                files.Add(new GitHubFileInfo
                {
                    Path = relativePath,
                    DownloadUrl = item.DownloadUrl ?? $"{RawGitHubBase}/{owner}/{repo}/{branch}/.github/{relativePath}"
                });
            }
            else if (item.Type == "dir" && !string.IsNullOrEmpty(item.Url))
            {
                // Fetch subdirectory contents
                var subResponse = await _httpClient.GetAsync(item.Url, ct);
                if (subResponse.IsSuccessStatusCode)
                {
                    var subJson = await subResponse.Content.ReadAsStringAsync(ct);
                    var subItems = JsonSerializer.Deserialize<GitHubContentItem[]>(subJson, JsonOptions);
                    if (subItems != null)
                    {
                        await CollectFilesRecursivelyAsync(owner, repo, branch, subItems, relativePath, files, ct);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Download raw file content from GitHub.
    /// </summary>
    /// <param name="downloadUrl">The raw download URL.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>File content or null if failed.</returns>
    public async Task<string?> DownloadFileAsync(string downloadUrl, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(downloadUrl, ct);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            return await response.Content.ReadAsStringAsync(ct);
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
