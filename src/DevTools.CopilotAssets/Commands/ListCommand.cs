using System.CommandLine;
using System.CommandLine.Invocation;
using DevTools.CopilotAssets.Domain;
using DevTools.CopilotAssets.Services;

namespace DevTools.CopilotAssets.Commands;

/// <summary>
/// List installed assets.
/// </summary>
public sealed class ListCommand : BaseCommand
{
    public static Command Create(IPolicyAppService policyService, Option<bool> globalJsonOption)
    {
        var typeOption = new Option<string?>(
            "--type",
            "Filter by asset type (instruction, prompts, agents, skills)");

        var pathArgument = new Argument<string>(
            "path",
            () => ".",
            "Target directory");

        var command = new Command("list", "List installed assets")
        {
            typeOption,
            pathArgument
        };

        command.SetHandler(async (InvocationContext ctx) =>
        {
            var type = ctx.ParseResult.GetValueForOption(typeOption);
            var path = ctx.ParseResult.GetValueForArgument(pathArgument);
            var json = ctx.ParseResult.GetValueForOption(globalJsonOption);
            JsonMode = json;

            // Parse type filter
            AssetTypeFilter? filter = null;
            if (!string.IsNullOrEmpty(type))
            {
                var parseResult = AssetTypeFilter.ParseOnly(type);
                if (!parseResult.Success)
                {
                    if (json)
                        WriteJson("list", new { }, 1, [parseResult.Error!]);
                    else
                        WriteError(parseResult.Error!);
                    Environment.ExitCode = 1;
                    return;
                }
                filter = parseResult.Filter;
            }

            var options = new ListOptions
            {
                TargetDirectory = path,
                Filter = filter
            };

            var result = await policyService.ListAssetsAsync(options);

            var hasInvalid = result.Summary.Modified > 0 || result.Summary.Missing > 0;
            var exitCode = hasInvalid ? 1 : 0;

            if (json)
            {
                WriteJson("list", result, exitCode);
            }
            else
            {
                PrintAssetTable(result);
            }

            Environment.ExitCode = exitCode;
        });

        return command;
    }

    private static void PrintAssetTable(AssetListResult result)
    {
        if (result.Assets.Count == 0)
        {
            Console.WriteLine("No assets installed.");
            Console.WriteLine();
            Console.WriteLine("Run 'copilot-assets init' to install assets.");
            return;
        }

        Console.WriteLine($"Installed Assets ({result.Summary.Total})");

        // Show template source
        if (result.Source != null)
        {
            var sourceDesc = result.Source.Type switch
            {
                "default" => "default templates",
                "remote" => $"remote: {result.Source.Repo}@{result.Source.Branch}",
                _ => result.Source.Type
            };
            Console.WriteLine($"Source: {sourceDesc}");
        }

        Console.WriteLine();
        Console.WriteLine("Type          Name                      Status");
        Console.WriteLine("────────────────────────────────────────────────────");

        foreach (var asset in result.Assets)
        {
            var status = asset.Valid ? "✓ valid" : $"✗ {asset.Reason}";
            Console.WriteLine($"{asset.Type,-13} {asset.Name,-25} {status}");
        }

        Console.WriteLine();
        if (result.Summary.Modified > 0 || result.Summary.Missing > 0)
        {
            Console.WriteLine($"Total: {result.Summary.Total} assets ({result.Summary.Valid} valid, {result.Summary.Modified} modified, {result.Summary.Missing} missing)");
        }
        else
        {
            Console.WriteLine($"Total: {result.Summary.Total} assets ({result.Summary.Valid} valid)");
        }
    }
}
