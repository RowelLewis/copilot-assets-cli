using DevTools.CopilotAssets.Domain.Configuration;
using DevTools.CopilotAssets.Services.Http;

namespace DevTools.CopilotAssets.Services.Templates;

/// <summary>
/// Provides templates from a remote GitHub repository.
/// Falls back to cached templates if network is unavailable.
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
        // If no remote configured, use bundled
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
            // Cache the templates locally
            await CacheTemplatesAsync(result.Templates, ct);
            return result with { Source = source };
        }

        // Try cached templates
        var cached = await GetCachedTemplatesAsync(ct);
        if (cached.HasTemplates)
        {
            return cached with { Source = $"cached:{_config.Source}@{_config.Branch}" };
        }

        // Fall back to bundled
        var bundled = await _fallbackProvider.GetTemplatesAsync(ct);
        return bundled with
        {
            Error = $"Remote unavailable ({result.Error}), using bundled templates"
        };
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

        return new TemplateResult(templates, $"remote:{owner}/{repo}@{branch}", FromCache: false);
    }

    private async Task CacheTemplatesAsync(IReadOnlyList<TemplateFile> templates, CancellationToken ct)
    {
        var cacheDir = GetCacheDirectory();

        // Clear existing cache
        if (_fileSystem.Exists(cacheDir))
        {
            try
            {
                Directory.Delete(cacheDir, recursive: true);
            }
            catch
            {
                // Ignore cache cleanup errors
            }
        }

        _fileSystem.CreateDirectory(cacheDir);

        foreach (var template in templates)
        {
            ct.ThrowIfCancellationRequested();

            var targetPath = Path.Combine(cacheDir, template.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            var targetDir = Path.GetDirectoryName(targetPath);

            if (!string.IsNullOrEmpty(targetDir))
            {
                _fileSystem.CreateDirectory(targetDir);
            }

            _fileSystem.WriteAllText(targetPath, template.Content);
        }
    }

    private Task<TemplateResult> GetCachedTemplatesAsync(CancellationToken ct)
    {
        var cacheDir = GetCacheDirectory();

        if (!_fileSystem.Exists(cacheDir))
        {
            return Task.FromResult(TemplateResult.Empty("cached"));
        }

        var templates = new List<TemplateFile>();
        var files = _fileSystem.GetFiles(cacheDir, "*", recursive: true);

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();

            var relativePath = Path.GetRelativePath(cacheDir, file)
                .Replace(Path.DirectorySeparatorChar, '/');
            var content = _fileSystem.ReadAllText(file);

            templates.Add(new TemplateFile(relativePath, content));
        }

        return Task.FromResult(new TemplateResult(templates, "cached", FromCache: true));
    }

    private static string GetCacheDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config",
            "copilot-assets",
            "cache",
            ".github");
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
