using System.CommandLine;
using System.CommandLine.Invocation;
using DevTools.CopilotAssets.Services.Fleet;

namespace DevTools.CopilotAssets.Commands;

/// <summary>
/// Fleet management command with subcommands: add, remove, list, sync, validate, status.
/// </summary>
public sealed class FleetCommand : BaseCommand
{
    public static Command Create(FleetManager fleetManager, FleetSyncService fleetSyncService, Option<bool> globalJsonOption)
    {
        var command = new Command("fleet", "Manage assets across multiple repositories");

        command.AddCommand(CreateAddCommand(fleetManager, globalJsonOption));
        command.AddCommand(CreateRemoveCommand(fleetManager, globalJsonOption));
        command.AddCommand(CreateListCommand(fleetManager, globalJsonOption));
        command.AddCommand(CreateValidateCommand(fleetSyncService, globalJsonOption));
        command.AddCommand(CreateStatusCommand(fleetSyncService, globalJsonOption));

        return command;
    }

    private static Command CreateAddCommand(FleetManager fleetManager, Option<bool> globalJsonOption)
    {
        var repoArgument = new Argument<string>("repo", "Repository in owner/repo format");
        var sourceOption = new Option<string?>("--source", "Template source for this repo");
        var targetOption = new Option<string?>("--target", "Target tools (comma-separated)");
        var branchOption = new Option<string?>("--branch", "Branch to target");

        var command = new Command("add", "Add a repository to the fleet")
        {
            repoArgument,
            sourceOption,
            targetOption,
            branchOption
        };

        command.SetHandler((InvocationContext ctx) =>
        {
            var repo = ctx.ParseResult.GetValueForArgument(repoArgument);
            var source = ctx.ParseResult.GetValueForOption(sourceOption);
            var target = ctx.ParseResult.GetValueForOption(targetOption);
            var branch = ctx.ParseResult.GetValueForOption(branchOption);
            var json = ctx.ParseResult.GetValueForOption(globalJsonOption);
            JsonMode = json;

            try
            {
                var config = fleetManager.AddRepo(repo, source, target, branch);

                if (json)
                {
                    WriteJson("fleet-add", new { repo, status = "added", total = config.Repos.Count });
                    return;
                }

                WriteSuccess($"Added '{repo}' to fleet ({config.Repos.Count} repos total)");
            }
            catch (Exception ex)
            {
                WriteError(ex.Message);
                ctx.ExitCode = 1;
            }
        });

        return command;
    }

    private static Command CreateRemoveCommand(FleetManager fleetManager, Option<bool> globalJsonOption)
    {
        var repoArgument = new Argument<string>("repo", "Repository to remove");

        var command = new Command("remove", "Remove a repository from the fleet")
        {
            repoArgument
        };

        command.SetHandler((InvocationContext ctx) =>
        {
            var repo = ctx.ParseResult.GetValueForArgument(repoArgument);
            var json = ctx.ParseResult.GetValueForOption(globalJsonOption);
            JsonMode = json;

            try
            {
                var config = fleetManager.RemoveRepo(repo);

                if (json)
                {
                    WriteJson("fleet-remove", new { repo, status = "removed", total = config.Repos.Count });
                    return;
                }

                WriteSuccess($"Removed '{repo}' from fleet ({config.Repos.Count} repos remaining)");
            }
            catch (Exception ex)
            {
                WriteError(ex.Message);
                ctx.ExitCode = 1;
            }
        });

        return command;
    }

    private static Command CreateListCommand(FleetManager fleetManager, Option<bool> globalJsonOption)
    {
        var command = new Command("list", "List all repositories in the fleet");

        command.SetHandler((InvocationContext ctx) =>
        {
            var json = ctx.ParseResult.GetValueForOption(globalJsonOption);
            JsonMode = json;

            var repos = fleetManager.ListRepos();

            if (json)
            {
                WriteJson("fleet-list", repos);
                return;
            }

            if (repos.Count == 0)
            {
                WriteInfo("No repositories in fleet. Use 'fleet add <owner/repo>' to add one.");
                return;
            }

            Console.WriteLine($"Fleet ({repos.Count} repos):\n");
            Console.WriteLine($"  {"Repository",-35} {"Source",-20} {"Targets",-25} Branch");
            Console.WriteLine($"  {new string('-', 35)} {new string('-', 20)} {new string('-', 25)} {new string('-', 10)}");

            var config = Domain.Fleet.FleetConfig.Load();
            foreach (var repo in repos)
            {
                var source = FleetManager.GetEffectiveSource(repo, config);
                var targets = string.Join(", ", FleetManager.GetEffectiveTargets(repo, config));
                var branch = FleetManager.GetEffectiveBranch(repo, config);
                Console.WriteLine($"  {repo.Name,-35} {source,-20} {targets,-25} {branch}");
            }
        });

        return command;
    }

    private static Command CreateValidateCommand(FleetSyncService fleetSyncService, Option<bool> globalJsonOption)
    {
        var command = new Command("validate", "Validate compliance across all fleet repositories");

        command.SetHandler(async (InvocationContext ctx) =>
        {
            var json = ctx.ParseResult.GetValueForOption(globalJsonOption);
            JsonMode = json;

            var report = await fleetSyncService.ValidateFleetAsync(ctx.GetCancellationToken());

            if (json)
            {
                WriteJson("fleet-validate", report);
                return;
            }

            Console.WriteLine($"Fleet Validation Report ({report.Total} repos):\n");

            foreach (var status in report.Repos)
            {
                var symbol = status.Status switch
                {
                    "compliant" => "+",
                    "non-compliant" => "!",
                    _ => "?"
                };
                Console.WriteLine($"  [{symbol}] {status.Repo} - {status.Status}");
                foreach (var error in status.Errors)
                    Console.WriteLine($"      Error: {error}");
                foreach (var warning in status.Warnings)
                    Console.WriteLine($"      Warning: {warning}");
            }

            Console.WriteLine();
            Console.WriteLine($"  Compliant: {report.Compliant} | Non-compliant: {report.NonCompliant} | Unreachable: {report.Unreachable}");

            ctx.ExitCode = report.NonCompliant > 0 ? 1 : 0;
        });

        return command;
    }

    private static Command CreateStatusCommand(FleetSyncService fleetSyncService, Option<bool> globalJsonOption)
    {
        var command = new Command("status", "Show sync status for all fleet repositories");

        command.SetHandler(async (InvocationContext ctx) =>
        {
            var json = ctx.ParseResult.GetValueForOption(globalJsonOption);
            JsonMode = json;

            var report = await fleetSyncService.PreviewSyncAsync(ctx.GetCancellationToken());

            if (json)
            {
                WriteJson("fleet-status", report);
                return;
            }

            Console.WriteLine($"Fleet Status ({report.Total} repos):\n");

            foreach (var status in report.Repos)
            {
                var symbol = status.Status switch
                {
                    "up-to-date" => "+",
                    "changes-pending" => "~",
                    _ => "?"
                };
                Console.WriteLine($"  [{symbol}] {status.Repo} - {status.Status}");
            }

            Console.WriteLine();
            Console.WriteLine($"  Up-to-date: {report.Compliant} | Changes pending: {report.NonCompliant} | Unreachable: {report.Unreachable}");
        });

        return command;
    }
}
