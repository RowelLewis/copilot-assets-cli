using DevTools.CopilotAssets.Domain;
using DevTools.CopilotAssets.Domain.Fleet;

namespace DevTools.CopilotAssets.Services.Fleet;

/// <summary>
/// Orchestrates sync and validation operations across fleet repositories.
/// </summary>
public sealed class FleetSyncService
{
    private readonly IPolicyAppService _policyService;

    public FleetSyncService(IPolicyAppService policyService)
    {
        _policyService = policyService;
    }

    /// <summary>
    /// Validate compliance across all repos in the fleet.
    /// </summary>
    public async Task<FleetReport> ValidateFleetAsync(CancellationToken ct = default)
    {
        var config = FleetConfig.Load();
        var report = new FleetReport { Total = config.Repos.Count };

        foreach (var repo in config.Repos)
        {
            var status = new FleetStatus
            {
                Repo = repo.Name,
                Status = "unknown"
            };

            try
            {
                // For fleet validation, we'd normally clone the repo and validate.
                // For now, this validates against local paths if they exist.
                var localPath = FindLocalRepoPath(repo.Name);
                if (localPath == null)
                {
                    status = status with { Status = "unreachable" };
                    status.Errors.Add($"Repository not found locally. Use 'fleet sync' to check remote repos.");
                    report.Unreachable++;
                }
                else
                {
                    var result = await _policyService.ValidateAsync(new ValidateOptions
                    {
                        TargetDirectory = localPath,
                        CiMode = false
                    });

                    if (result.IsCompliant)
                    {
                        status = status with { Status = "compliant" };
                        report.Compliant++;
                    }
                    else
                    {
                        status = status with { Status = "non-compliant" };
                        status.Errors.AddRange(result.Errors);
                        status.Warnings.AddRange(result.Warnings);
                        report.NonCompliant++;
                    }
                }
            }
            catch (Exception ex)
            {
                status = status with { Status = "error" };
                status.Errors.Add(ex.Message);
                report.Unreachable++;
            }

            report.Repos.Add(status);
        }

        return report;
    }

    /// <summary>
    /// Preview sync operations for all fleet repos (dry-run).
    /// </summary>
    public async Task<FleetReport> PreviewSyncAsync(CancellationToken ct = default)
    {
        var config = FleetConfig.Load();
        var report = new FleetReport { Total = config.Repos.Count };

        foreach (var repo in config.Repos)
        {
            var status = new FleetStatus
            {
                Repo = repo.Name,
                Status = "unknown"
            };

            var localPath = FindLocalRepoPath(repo.Name);
            if (localPath == null)
            {
                status = status with { Status = "unreachable" };
                status.Errors.Add("Repository not found locally");
                report.Unreachable++;
            }
            else
            {
                var source = FleetManager.GetEffectiveSource(repo, config);
                var targets = FleetManager.GetEffectiveTargets(repo, config);
                var targetTools = targets
                    .Select(t => Enum.TryParse<TargetTool>(t, true, out var tool) ? tool : (TargetTool?)null)
                    .Where(t => t != null)
                    .Select(t => t!.Value)
                    .ToList();

                var dryRun = await _policyService.PreviewInitAsync(new InitOptions
                {
                    TargetDirectory = localPath,
                    SourceOverride = source == "default" ? null : source,
                    Targets = targetTools.Count > 0 ? targetTools : null
                });

                var pendingChanges = dryRun.Operations.Count(o =>
                    o.Type == OperationType.Create ||
                    o.Type == OperationType.Update);

                if (pendingChanges > 0)
                {
                    status = status with { Status = "changes-pending" };
                    report.NonCompliant++;
                }
                else
                {
                    status = status with { Status = "up-to-date" };
                    report.Compliant++;
                }
            }

            report.Repos.Add(status);
        }

        return report;
    }

    /// <summary>
    /// Try to find a local path for a repo (convention: ~/repos/owner/repo or current siblings).
    /// </summary>
    private static string? FindLocalRepoPath(string repoName)
    {
        // Try common locations
        var parts = repoName.Split('/');
        if (parts.Length != 2) return null;

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var candidates = new[]
        {
            Path.Combine(home, "repos", parts[0], parts[1]),
            Path.Combine(home, "dev", parts[0], parts[1]),
            Path.Combine(home, "Dev", parts[0], parts[1]),
            Path.Combine(home, "projects", parts[0], parts[1]),
            Path.Combine(home, "Projects", parts[0], parts[1]),
            Path.Combine(home, "src", parts[0], parts[1]),
            Path.Combine(Environment.CurrentDirectory, "..", parts[1])
        };

        return candidates.FirstOrDefault(Directory.Exists);
    }
}
