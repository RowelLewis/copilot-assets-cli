using System.CommandLine;
using System.CommandLine.Invocation;
using DevTools.CopilotAssets.Services;

namespace DevTools.CopilotAssets.Commands;

/// <summary>
/// Display version information.
/// </summary>
public sealed class VersionCommand : BaseCommand
{
    public static Command Create(Option<bool> globalJsonOption)
    {
        var command = new Command("version", "Display version information");

        command.SetHandler((InvocationContext ctx) =>
        {
            var json = ctx.ParseResult.GetValueForOption(globalJsonOption);
            JsonMode = json;

            if (json)
            {
                WriteJson("version", new
                {
                    tool = "copilot-assets",
                    version = SyncEngine.ToolVersion,
                    assetVersion = SyncEngine.AssetVersion
                });
            }
            else
            {
                Console.WriteLine($"copilot-assets {SyncEngine.ToolVersion}");
                Console.WriteLine($"Asset version: {SyncEngine.AssetVersion}");
            }
        });

        return command;
    }
}
