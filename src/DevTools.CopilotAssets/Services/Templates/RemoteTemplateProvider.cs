using DevTools.CopilotAssets.Domain.Configuration;
using DevTools.CopilotAssets.Services.Http;

namespace DevTools.CopilotAssets.Services.Templates;

/// <summary>
/// Provides templates from a remote GitHub repository.
/// Falls back to default templates if remote is unavailable.
/// </summary>
public sealed class RemoteTemplateProvider : ITemplateProvider, IDisposable
{
    private readonly RemoteConfig _config;
    private readonly GitHubClient _gitHubClient;
    private readonly IFileSystemService _fileSystem;
    private readonly BundledTemplateProvider _fallbackProvider;

    public RemoteTemplateProvider(
        RemoteConfig config,
        IFileSystemService fileSystem,
        GitHubClient? gitHubClient = null)
    {
        _config = config;
        _fileSystem = fileSystem;
        _gitHubClient = gitHubClient ?? new GitHubClient();
        _fallbackProvider = new BundledTemplateProvider(fileSystem);
    }

    /// <inheritdoc />
    public async Task<TemplateResult> GetTemplatesAsync(CancellationToken ct = default)
    {
        // If no remote configured, use default templates
        if (!_config.HasRemoteSource || string.IsNullOrEmpty(_config.Source))
        {
            return await _fallbackProvider.GetTemplatesAsync(ct);
        }

        var (owner, repo) = ParseSource(_config.Source);
        var source = $"remote:{_config.Source}@{_config.Branch}";

        // Try to fetch from remote
        var result = await FetchFromRemoteAsync(owner, repo, _config.Branch, ct);

        if (result.HasTemplates)
        {
            return result with { Source = source };
        }

        // Remote failed - return error, don't silently fall back
        // The caller should handle this and offer options to the user
        return TemplateResult.Failed(source, result.Error ?? "Failed to fetch remote templates");
    }

    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        if (!_config.HasRemoteSource || string.IsNullOrEmpty(_config.Source))
        {
            return false;
        }

        var (owner, repo) = ParseSource(_config.Source);
        var result = await _gitHubClient.GetDirectoryContentsAsync(owner, repo, ".github", _config.Branch, ct);
        return result.Success;
    }

    private async Task<TemplateResult> FetchFromRemoteAsync(
        string owner,
        string repo,
        string branch,
        CancellationToken ct)
    {
        var directoryResult = await _gitHubClient.GetDirectoryContentsAsync(owner, repo, ".github", branch, ct);

        if (!directoryResult.Success)
        {
            return TemplateResult.Failed($"remote:{owner}/{repo}@{branch}", directoryResult.Error ?? "Unknown error");
        }

        var templates = new List<TemplateFile>();

        foreach (var file in directoryResult.Files)
        {
            ct.ThrowIfCancellationRequested();

            var content = await _gitHubClient.DownloadFileAsync(file.DownloadUrl, ct);
            if (content != null)
            {
                templates.Add(new TemplateFile(file.Path, content));
            }
        }

        if (templates.Count == 0)
        {
            return TemplateResult.Failed($"remote:{owner}/{repo}@{branch}", "No template files found in .github directory");
        }

        return new TemplateResult(templates, $"remote:{owner}/{repo}@{branch}");
    }

    private static (string Owner, string Repo) ParseSource(string source)
    {
        var parts = source.Split('/');
        return (parts[0], parts[1]);
    }

    public void Dispose()
    {
        _gitHubClient.Dispose();
    }
}
