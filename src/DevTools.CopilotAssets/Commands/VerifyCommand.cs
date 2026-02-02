using System.CommandLine;
using System.CommandLine.Invocation;
using DevTools.CopilotAssets.Domain;
using DevTools.CopilotAssets.Services;

namespace DevTools.CopilotAssets.Commands;

/// <summary>
/// Verify file integrity against manifest checksums.
/// </summary>
public sealed class VerifyCommand : BaseCommand
{
    public static Command Create(IPolicyAppService policyService, Option<bool> globalJsonOption)
    {
        var restoreOption = new Option<bool>(
            "--restore",
            "Restore modified files to original state");

        var onlyOption = new Option<string?>(
            "--only",
            "Verify only specified asset type (instruction, prompts, agents, skills)");

        var pathArgument = new Argument<string>(
            "path",
            () => ".",
            "Target directory");

        var command = new Command("verify", "Check file integrity against manifest checksums")
        {
            restoreOption,
            onlyOption,
            pathArgument
        };

        command.SetHandler(async (InvocationContext ctx) =>
        {
            var restore = ctx.ParseResult.GetValueForOption(restoreOption);
            var only = ctx.ParseResult.GetValueForOption(onlyOption);
            var path = ctx.ParseResult.GetValueForArgument(pathArgument);
            var json = ctx.ParseResult.GetValueForOption(globalJsonOption);
            JsonMode = json;

            // Parse filter
            AssetTypeFilter? filter = null;
            if (!string.IsNullOrEmpty(only))
            {
                var parseResult = AssetTypeFilter.ParseOnly(only);
                if (!parseResult.Success)
                {
                    if (json)
                        WriteJson("verify", new { }, 1, [parseResult.Error!]);
                    else
                        WriteError(parseResult.Error!);
                    Environment.ExitCode = 1;
                    return;
                }
                filter = parseResult.Filter;
            }

            var options = new VerifyOptions
            {
                TargetDirectory = path,
                Restore = restore,
                Filter = filter
            };

            var result = await policyService.VerifyAsync(options);

            if (json)
            {
                WriteJson("verify", new
                {
                    assets = result.Assets,
                    summary = result.Summary
                }, result.ExitCode, result.Errors, result.Warnings);
            }
            else
            {
                if (result.Errors.Count > 0)
                {
                    foreach (var error in result.Errors)
                        WriteError(error);
                    Environment.ExitCode = result.ExitCode;
                    return;
                }

                PrintVerifyTable(result);
            }

            Environment.ExitCode = result.ExitCode;
        });

        return command;
    }

    private static void PrintVerifyTable(VerifyResult result)
    {
        if (result.Assets.Count == 0)
        {
            Console.WriteLine("No assets to verify.");
            Console.WriteLine();
            Console.WriteLine("Run 'copilot-assets init' to install assets first.");
            return;
        }

        Console.WriteLine($"Verifying {result.Assets.Count} assets...");
        Console.WriteLine();

        foreach (var asset in result.Assets)
        {
            var symbol = asset.Status switch
            {
                VerifyStatus.Valid => "✓",
                VerifyStatus.Modified => "✗",
                VerifyStatus.Missing => "✗",
                VerifyStatus.Restored => "↺",
                _ => "?"
            };

            var status = asset.Status switch
            {
                VerifyStatus.Valid => "",
                VerifyStatus.Modified => "MODIFIED",
                VerifyStatus.Missing => "MISSING",
                VerifyStatus.Restored => "RESTORED",
                _ => ""
            };

            Console.WriteLine($"{symbol} {asset.Name,-30} {status}");
        }

        Console.WriteLine();

        var validCount = result.Summary.Valid + result.Summary.Restored;
        var invalidCount = result.Summary.Modified + result.Summary.Missing;

        if (result.Summary.Restored > 0)
        {
            Console.WriteLine($"{validCount}/{result.Assets.Count} valid ({result.Summary.Restored} restored)");
        }
        else if (invalidCount > 0)
        {
            Console.WriteLine($"{result.Summary.Valid}/{result.Assets.Count} valid, {invalidCount} invalid");
        }
        else
        {
            Console.WriteLine($"{result.Summary.Valid}/{result.Assets.Count} valid");
        }
    }
}
