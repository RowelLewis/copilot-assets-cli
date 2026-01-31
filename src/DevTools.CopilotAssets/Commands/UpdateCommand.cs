using System.CommandLine;
using System.CommandLine.Invocation;
using DevTools.CopilotAssets.Domain;
using DevTools.CopilotAssets.Services;

namespace DevTools.CopilotAssets.Commands;

/// <summary>
/// Update existing assets to latest version.
/// </summary>
public sealed class UpdateCommand : BaseCommand
{
    public static Command Create(IPolicyAppService policyService, Option<bool> globalJsonOption)
    {
        var forceOption = new Option<bool>(
            ["--force", "-f"],
            "Force update even if already at latest version");

        var noGitOption = new Option<bool>(
            "--no-git",
            "Skip git operations");

        var dryRunOption = new Option<bool>(
            "--dry-run",
            "Preview changes without making modifications");

        var onlyOption = new Option<string?>(
            "--only",
            "Update only specified asset types (comma-separated: instruction,prompts,agents,skills)");

        var excludeOption = new Option<string?>(
            "--exclude",
            "Exclude specified asset types (comma-separated: instruction,prompts,agents,skills)");

        var pathArgument = new Argument<string>(
            "path",
            () => ".",
            "Target directory");

        var command = new Command("update", "Update assets to latest version")
        {
            forceOption,
            noGitOption,
            dryRunOption,
            onlyOption,
            excludeOption,
            pathArgument
        };

        command.SetHandler(async (InvocationContext ctx) =>
        {
            var force = ctx.ParseResult.GetValueForOption(forceOption);
            var noGit = ctx.ParseResult.GetValueForOption(noGitOption);
            var dryRun = ctx.ParseResult.GetValueForOption(dryRunOption);
            var only = ctx.ParseResult.GetValueForOption(onlyOption);
            var exclude = ctx.ParseResult.GetValueForOption(excludeOption);
            var path = ctx.ParseResult.GetValueForArgument(pathArgument);
            var json = ctx.ParseResult.GetValueForOption(globalJsonOption);
            JsonMode = json;

            // Validate mutually exclusive options
            if (!string.IsNullOrEmpty(only) && !string.IsNullOrEmpty(exclude))
            {
                if (json)
                    WriteJson("update", new { }, 1, ["Cannot use --only and --exclude together"]);
                else
                    WriteError("Cannot use --only and --exclude together");
                Environment.ExitCode = 1;
                return;
            }

            // Parse filter
            AssetTypeFilter? filter = null;
            if (!string.IsNullOrEmpty(only))
            {
                var parseResult = AssetTypeFilter.ParseOnly(only);
                if (!parseResult.Success)
                {
                    if (json)
                        WriteJson("update", new { }, 1, [parseResult.Error!]);
                    else
                        WriteError(parseResult.Error!);
                    Environment.ExitCode = 1;
                    return;
                }
                filter = parseResult.Filter;
            }
            else if (!string.IsNullOrEmpty(exclude))
            {
                var parseResult = AssetTypeFilter.ParseExclude(exclude);
                if (!parseResult.Success)
                {
                    if (json)
                        WriteJson("update", new { }, 1, [parseResult.Error!]);
                    else
                        WriteError(parseResult.Error!);
                    Environment.ExitCode = 1;
                    return;
                }
                filter = parseResult.Filter;
            }

            var options = new UpdateOptions
            {
                TargetDirectory = path,
                Force = force,
                NoGit = noGit,
                Filter = filter
            };

            // Dry run mode
            if (dryRun)
            {
                var preview = await policyService.PreviewUpdateAsync(options);
                if (json)
                {
                    WriteJson("update", preview, preview.ExitCode);
                }
                else
                {
                    PrintDryRunResult(preview);
                }
                Environment.ExitCode = preview.ExitCode;
                return;
            }

            var result = await policyService.UpdateAsync(options);

            if (json)
            {
                WriteJson("update", new
                {
                    projectPath = path,
                    updated = result.IsCompliant
                }, result.IsCompliant ? 0 : 1, result.Errors, result.Warnings);
            }
            else
            {
                foreach (var error in result.Errors)
                    WriteError(error);
                foreach (var warning in result.Warnings)
                    WriteWarning(warning);
                foreach (var info in result.Info)
                    WriteInfo(info);
            }

            Environment.ExitCode = result.IsCompliant ? 0 : 1;
        });

        return command;
    }

    private static void PrintDryRunResult(DryRunResult result)
    {
        Console.WriteLine("Dry Run - No changes will be made");
        Console.WriteLine();

        if (result.Operations.Count == 0 ||
            result.Operations.All(o => o.Type == OperationType.Skip))
        {
            var skipReason = result.Operations.FirstOrDefault()?.Reason ?? "No changes needed";
            Console.WriteLine(skipReason);
            return;
        }

        var updates = result.Operations.Where(o => o.Type == OperationType.Update).ToList();
        var creates = result.Operations.Where(o => o.Type == OperationType.Create).ToList();

        if (updates.Count > 0)
        {
            Console.WriteLine("Would update:");
            foreach (var op in updates)
                Console.WriteLine($"  {op.Path} ({op.Reason})");
            Console.WriteLine();
        }

        if (creates.Count > 0)
        {
            Console.WriteLine("Would create:");
            foreach (var op in creates)
                Console.WriteLine($"  {op.Path}");
            Console.WriteLine();
        }

        Console.WriteLine($"Summary: {result.Summary.Creates} creates, {result.Summary.Updates} updates");
    }
}
