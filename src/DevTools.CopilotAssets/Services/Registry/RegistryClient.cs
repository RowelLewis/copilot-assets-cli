using System.Text.Json;
using DevTools.CopilotAssets.Domain.Registry;
using DevTools.CopilotAssets.Services.Http;

namespace DevTools.CopilotAssets.Services.Registry;

/// <summary>
/// Client for fetching and searching the template pack registry.
/// </summary>
public sealed class RegistryClient
{
    private readonly GitHubClient _gitHubClient;

    /// <summary>
    /// Default registry repository.
    /// </summary>
    public const string DefaultRegistryRepo = "copilot-assets/registry";
    public const string DefaultRegistryBranch = "main";
    private const string IndexFileName = "index.json";

    private RegistryIndex? _cachedIndex;

    public RegistryClient(GitHubClient gitHubClient)
    {
        _gitHubClient = gitHubClient;
    }

    /// <summary>
    /// Fetch the registry index.
    /// </summary>
    public async Task<RegistryIndex?> GetIndexAsync(CancellationToken ct = default)
    {
        if (_cachedIndex != null) return _cachedIndex;

        try
        {
            var url = $"https://raw.githubusercontent.com/{DefaultRegistryRepo}/{DefaultRegistryBranch}/{IndexFileName}";
            var content = await _gitHubClient.DownloadFileAsync(url, ct);

            if (string.IsNullOrEmpty(content))
                return null;

            _cachedIndex = JsonSerializer.Deserialize<RegistryIndex>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return _cachedIndex;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Search packs by query (matches name, description, tags).
    /// </summary>
    public async Task<IReadOnlyList<PackMetadata>> SearchAsync(string query, CancellationToken ct = default)
    {
        var index = await GetIndexAsync(ct);
        if (index == null) return [];

        var lowerQuery = query.ToLowerInvariant();

        return index.Packs
            .Where(p =>
                p.Name.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ||
                p.Tags.Any(t => t.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    /// <summary>
    /// Get a specific pack by name.
    /// </summary>
    public async Task<PackMetadata?> GetPackAsync(string name, CancellationToken ct = default)
    {
        var index = await GetIndexAsync(ct);
        return index?.Packs.FirstOrDefault(p =>
            p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// List all available packs.
    /// </summary>
    public async Task<IReadOnlyList<PackMetadata>> ListAsync(CancellationToken ct = default)
    {
        var index = await GetIndexAsync(ct);
        return index?.Packs ?? [];
    }

    /// <summary>
    /// Load a registry index from JSON content (for testing).
    /// </summary>
    public void LoadFromJson(string json)
    {
        _cachedIndex = JsonSerializer.Deserialize<RegistryIndex>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}
