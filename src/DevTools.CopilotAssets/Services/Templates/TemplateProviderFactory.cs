using DevTools.CopilotAssets.Domain.Configuration;
using DevTools.CopilotAssets.Services.Http;

namespace DevTools.CopilotAssets.Services.Templates;

/// <summary>
/// Factory for creating template providers based on configuration.
/// </summary>
public sealed class TemplateProviderFactory : IDisposable
{
    private readonly IFileSystemService _fileSystem;
    private readonly GitHubClient _gitHubClient;
    private readonly BundledTemplateProvider _bundledProvider;

    public TemplateProviderFactory(
        IFileSystemService fileSystem,
        GitHubClient gitHubClient,
        BundledTemplateProvider bundledProvider)
    {
        _fileSystem = fileSystem;
        _gitHubClient = gitHubClient;
        _bundledProvider = bundledProvider;
    }

    /// <summary>
    /// Create a template provider for the default (bundled) templates.
    /// </summary>
    public ITemplateProvider CreateDefaultProvider()
    {
        return _bundledProvider;
    }

    /// <summary>
    /// Create a template provider for a remote repository.
    /// </summary>
    /// <param name="repository">Repository in "owner/repo" format</param>
    /// <param name="branch">Branch name (defaults to "main")</param>
    public ITemplateProvider CreateRemoteProvider(string repository, string? branch = null)
    {
        var config = new RemoteConfig
        {
            Source = repository,
            Branch = branch ?? "main"
        };
        return new RemoteTemplateProvider(config, _fileSystem, _gitHubClient);
    }

    /// <summary>
    /// Create a template provider based on the persisted configuration.
    /// </summary>
    public ITemplateProvider CreateFromConfig()
    {
        var config = RemoteConfig.Load();
        if (config.HasRemoteSource)
        {
            return new RemoteTemplateProvider(config, _fileSystem, _gitHubClient);
        }
        return _bundledProvider;
    }

    /// <summary>
    /// Create a template provider based on the given source specification.
    /// </summary>
    /// <param name="source">Source: "default" for bundled, or "owner/repo[@branch]" for remote</param>
    public ITemplateProvider CreateFromSource(string? source)
    {
        if (string.IsNullOrEmpty(source) ||
            source.Equals("default", StringComparison.OrdinalIgnoreCase) ||
            source.Equals("bundled", StringComparison.OrdinalIgnoreCase))
        {
            return _bundledProvider;
        }

        // Parse owner/repo[@branch] format
        var parts = source.Split('@', 2);
        var repository = parts[0];
        var branch = parts.Length > 1 ? parts[1] : "main";

        return CreateRemoteProvider(repository, branch);
    }

    public void Dispose()
    {
        _gitHubClient.Dispose();
    }
}
