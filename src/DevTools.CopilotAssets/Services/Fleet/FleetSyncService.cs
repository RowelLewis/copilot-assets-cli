using System.Diagnostics;
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
    /// Sync assets to all repos in the fleet.
    /// </summary>
    public async Task<FleetReport> SyncFleetAsync(bool dryRun = false, bool createPr = false, CancellationToken ct = default)
    {
        if (dryRun)
            return await PreviewSyncAsync(ct);

        var config = FleetConfig.Load();
        var report = new FleetReport { Total = config.Repos.Count };
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        foreach (var repo in config.Repos)
        {
            var status = new FleetStatus
            {
                Repo = repo.Name,
                Status = "unknown"
            };

            try
            {
                var localPath = FindLocalRepoPath(repo.Name);
                if (localPath == null)
                {
                    status = status with { Status = "unreachable" };
                    status.Errors.Add("Repository not found locally. Clone the repo first.");
                    report.Unreachable++;
                    report.Repos.Add(status);
                    continue;
                }

                var source = FleetManager.GetEffectiveSource(repo, config);
                var targets = FleetManager.GetEffectiveTargets(repo, config);
                var branch = FleetManager.GetEffectiveBranch(repo, config);
                var targetTools = targets
                    .Select(t => Enum.TryParse<TargetTool>(t, true, out var tool) ? tool : (TargetTool?)null)
                    .Where(t => t != null)
                    .Select(t => t!.Value)
                    .ToList();

                if (createPr)
                {
                    var prBranch = $"copilot-assets/update-{timestamp}";

                    // Ensure we're on the right branch and up to date
                    await RunCommandAsync("git", $"-C \"{localPath}\" checkout {branch}", ct);
                    await RunCommandAsync("git", $"-C \"{localPath}\" pull origin {branch}", ct);
                    await RunCommandAsync("git", $"-C \"{localPath}\" checkout -b {prBranch}", ct);

                    // Run sync
                    await _policyService.InitAsync(new InitOptions
                    {
                        TargetDirectory = localPath,
                        Force = true,
                        SourceOverride = source == "default" ? null : source,
                        Targets = targetTools.Count > 0 ? targetTools : null
                    });

                    // Check for changes
                    var (diffOutput, _) = await RunCommandAsync("git", $"-C \"{localPath}\" diff --name-only HEAD", ct);
                    var (untrackedOutput, _) = await RunCommandAsync("git", $"-C \"{localPath}\" ls-files --others --exclude-standard", ct);
                    var hasChanges = !string.IsNullOrWhiteSpace(diffOutput) || !string.IsNullOrWhiteSpace(untrackedOutput);

                    if (!hasChanges)
                    {
                        await RunCommandAsync("git", $"-C \"{localPath}\" checkout {branch}", ct);
                        await RunCommandAsync("git", $"-C \"{localPath}\" branch -D {prBranch}", ct);
                        status = status with { Status = "up-to-date" };
                        report.Compliant++;
                    }
                    else
                    {
                        await RunCommandAsync("git", $"-C \"{localPath}\" add -A", ct);
                        await RunCommandAsync("git", $"-C \"{localPath}\" commit -m \"chore: sync copilot assets via copilot-assets-cli\"", ct);
                        await RunCommandAsync("git", $"-C \"{localPath}\" push origin {prBranch}", ct);

                        var prBody = "Automated sync of copilot assets via copilot-assets-cli.\n\nThis PR was created by `copilot-assets fleet sync --pr`.";
                        var (prUrl, _) = await RunCommandAsync("gh",
                            $"pr create --repo \"{repo.Name}\" --base \"{branch}\" --head \"{prBranch}\" --title \"chore: sync copilot assets\" --body \"{prBody}\"",
                            ct);

                        status = status with { Status = "pr-created" };
                        if (!string.IsNullOrWhiteSpace(prUrl))
                            status.Warnings.Add($"PR: {prUrl.Trim()}");
                        report.NonCompliant++;
                    }
                }
                else
                {
                    // Direct sync
                    var result = await _policyService.InitAsync(new InitOptions
                    {
                        TargetDirectory = localPath,
                        Force = true,
                        SourceOverride = source == "default" ? null : source,
                        Targets = targetTools.Count > 0 ? targetTools : null
                    });

                    if (result.IsCompliant)
                    {
                        status = status with { Status = "synced" };
                        report.Compliant++;
                    }
                    else
                    {
                        status = status with { Status = "sync-failed" };
                        status.Errors.AddRange(result.Errors);
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
    /// Run a shell command and return (stdout, exitCode).
    /// </summary>
    private static async Task<(string Output, int ExitCode)> RunCommandAsync(
        string fileName, string arguments, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start process: {fileName}");

        var output = await process.StandardOutput.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        return (output, process.ExitCode);
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
