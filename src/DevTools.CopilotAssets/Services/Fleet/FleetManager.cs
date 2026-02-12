using DevTools.CopilotAssets.Domain.Fleet;
using DevTools.CopilotAssets.Infrastructure.Security;

namespace DevTools.CopilotAssets.Services.Fleet;

/// <summary>
/// Manages fleet configuration and operations across multiple repositories.
/// </summary>
public sealed class FleetManager
{
    /// <summary>
    /// Add a repository to the fleet.
    /// </summary>
    public FleetConfig AddRepo(string repoName, string? source = null, string? targets = null, string? branch = null)
    {
        if (!InputValidator.IsValidRepository(repoName))
            throw new ArgumentException($"Invalid repository format: {repoName}. Expected: owner/repo");

        var config = FleetConfig.Load();

        if (config.Repos.Any(r => r.Name.Equals(repoName, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Repository '{repoName}' is already in the fleet");

        var repo = new FleetRepo
        {
            Name = repoName,
            Source = source,
            Targets = targets?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(t => t.ToLowerInvariant()).ToList(),
            Branch = branch
        };

        config.Repos.Add(repo);
        config.Save();
        return config;
    }

    /// <summary>
    /// Remove a repository from the fleet.
    /// </summary>
    public FleetConfig RemoveRepo(string repoName)
    {
        var config = FleetConfig.Load();
        var existing = config.Repos.FirstOrDefault(r =>
            r.Name.Equals(repoName, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
            throw new InvalidOperationException($"Repository '{repoName}' is not in the fleet");

        config.Repos.Remove(existing);
        config.Save();
        return config;
    }

    /// <summary>
    /// List all repos in the fleet.
    /// </summary>
    public IReadOnlyList<FleetRepo> ListRepos()
    {
        return FleetConfig.Load().Repos;
    }

    /// <summary>
    /// Get the effective source for a repo (repo-specific or fleet default).
    /// </summary>
    public static string GetEffectiveSource(FleetRepo repo, FleetConfig config)
    {
        return repo.Source ?? config.Defaults.Source;
    }

    /// <summary>
    /// Get the effective targets for a repo (repo-specific or fleet default).
    /// </summary>
    public static IReadOnlyList<string> GetEffectiveTargets(FleetRepo repo, FleetConfig config)
    {
        return repo.Targets ?? config.Defaults.Targets;
    }

    /// <summary>
    /// Get the effective branch for a repo (repo-specific or fleet default).
    /// </summary>
    public static string GetEffectiveBranch(FleetRepo repo, FleetConfig config)
    {
        return repo.Branch ?? config.Defaults.Branch;
    }
}
