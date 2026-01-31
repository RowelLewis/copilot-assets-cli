using System.CommandLine;
using System.CommandLine.Invocation;
using DevTools.CopilotAssets.Domain;
using DevTools.CopilotAssets.Services;

namespace DevTools.CopilotAssets.Commands;

/// <summary>
/// Initialize project with Copilot assets.
/// </summary>
public sealed class InitCommand : BaseCommand
{
    public static Command Create(IPolicyAppService policyService, Option<bool> globalJsonOption)
    {
        var forceOption = new Option<bool>(
            ["--force", "-f"],
            "Overwrite existing files without prompting");

        var noGitOption = new Option<bool>(
            "--no-git",
            "Skip git operations (stage/commit)");

        var dryRunOption = new Option<bool>(
            "--dry-run",
            "Preview changes without making modifications");

        var onlyOption = new Option<string?>(
            "--only",
            "Install only specified asset types (comma-separated: instruction,prompts,agents,skills)");

        var excludeOption = new Option<string?>(
            "--exclude",
            "Exclude specified asset types (comma-separated: instruction,prompts,agents,skills)");

        var pathArgument = new Argument<string>(
            "path",
            () => ".",
            "Target directory (defaults to current directory)");

        var command = new Command("init", "Initialize project with Copilot assets")
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
                    WriteJson("init", new { }, 1, ["Cannot use --only and --exclude together"]);
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
                        WriteJson("init", new { }, 1, [parseResult.Error!]);
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
                        WriteJson("init", new { }, 1, [parseResult.Error!]);
                    else
                        WriteError(parseResult.Error!);
                    Environment.ExitCode = 1;
                    return;
                }
                filter = parseResult.Filter;
            }

            var options = new InitOptions
            {
                TargetDirectory = path,
                Force = force,
                NoGit = noGit,
                Filter = filter
            };

            // Dry run mode
            if (dryRun)
            {
                var preview = await policyService.PreviewInitAsync(options);
                if (json)
                {
                    WriteJson("init", preview, preview.ExitCode);
                }
                else
                {
                    PrintDryRunResult(preview);
                }
                Environment.ExitCode = preview.ExitCode;
                return;
            }

            var result = await policyService.InitAsync(options);

            if (json)
            {
                WriteJson("init", new
                {
                    projectPath = path,
                    assetsInstalled = result.Info.Count > 0
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

        if (result.Operations.Count == 0)
        {
            Console.WriteLine("No changes needed.");
            return;
        }

        var creates = result.Operations.Where(o => o.Type == OperationType.Create).ToList();
        var updates = result.Operations.Where(o => o.Type == OperationType.Update).ToList();
        var modifies = result.Operations.Where(o => o.Type == OperationType.Modify).ToList();
        var skips = result.Operations.Where(o => o.Type == OperationType.Skip && o.Reason != "unchanged").ToList();

        if (creates.Count > 0)
        {
            Console.WriteLine("Would create:");
            foreach (var op in creates)
                Console.WriteLine($"  {op.Path}");
            Console.WriteLine();
        }

        if (updates.Count > 0)
        {
            Console.WriteLine("Would update:");
            foreach (var op in updates)
                Console.WriteLine($"  {op.Path} ({op.Reason})");
            Console.WriteLine();
        }

        if (modifies.Count > 0)
        {
            Console.WriteLine("Would modify:");
            foreach (var op in modifies)
                Console.WriteLine($"  {op.Path} ({op.Reason})");
            Console.WriteLine();
        }

        if (skips.Count > 0)
        {
            Console.WriteLine("Would skip:");
            foreach (var op in skips)
                Console.WriteLine($"  {op.Path} ({op.Reason})");
            Console.WriteLine();
        }

        Console.WriteLine($"Summary: {result.Summary.Creates} creates, {result.Summary.Updates} updates, {result.Summary.Modifies} modifies");
    }
}
