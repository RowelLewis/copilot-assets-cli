using System.CommandLine;
using System.CommandLine.Invocation;
using DevTools.CopilotAssets.Domain.Configuration;
using DevTools.CopilotAssets.Services.Registry;

namespace DevTools.CopilotAssets.Commands;

/// <summary>
/// Registry command with subcommands: search, info, install, list.
/// </summary>
public sealed class RegistryCommand : BaseCommand
{
    public static Command Create(RegistryClient registryClient, Option<bool> globalJsonOption)
    {
        var command = new Command("registry", "Discover and install template packs from the community registry");

        command.AddCommand(CreateSearchCommand(registryClient, globalJsonOption));
        command.AddCommand(CreateInfoCommand(registryClient, globalJsonOption));
        command.AddCommand(CreateInstallCommand(registryClient, globalJsonOption));
        command.AddCommand(CreateListCommand(registryClient, globalJsonOption));

        return command;
    }

    private static Command CreateSearchCommand(RegistryClient registryClient, Option<bool> globalJsonOption)
    {
        var queryArgument = new Argument<string>(
            "query",
            "Search query (matches pack name, description, and tags)");

        var command = new Command("search", "Search for template packs")
        {
            queryArgument
        };

        command.SetHandler(async (InvocationContext ctx) =>
        {
            var query = ctx.ParseResult.GetValueForArgument(queryArgument);
            var json = ctx.ParseResult.GetValueForOption(globalJsonOption);
            JsonMode = json;

            var results = await registryClient.SearchAsync(query, ctx.GetCancellationToken());

            if (json)
            {
                WriteJson("registry-search", results);
                return;
            }

            if (results.Count == 0)
            {
                WriteWarning($"No packs found matching '{query}'");
                return;
            }

            Console.WriteLine($"Found {results.Count} pack(s):\n");
            foreach (var pack in results)
            {
                Console.WriteLine($"  {pack.Name} ({pack.Version})");
                Console.WriteLine($"    {pack.Description}");
                if (pack.Tags.Count > 0)
                    Console.WriteLine($"    Tags: {string.Join(", ", pack.Tags)}");
                Console.WriteLine($"    Targets: {string.Join(", ", pack.Targets)}");
                Console.WriteLine();
            }
        });

        return command;
    }

    private static Command CreateInfoCommand(RegistryClient registryClient, Option<bool> globalJsonOption)
    {
        var nameArgument = new Argument<string>(
            "name",
            "Pack name to get info for");

        var command = new Command("info", "Show detailed information about a template pack")
        {
            nameArgument
        };

        command.SetHandler(async (InvocationContext ctx) =>
        {
            var name = ctx.ParseResult.GetValueForArgument(nameArgument);
            var json = ctx.ParseResult.GetValueForOption(globalJsonOption);
            JsonMode = json;

            var pack = await registryClient.GetPackAsync(name, ctx.GetCancellationToken());

            if (pack == null)
            {
                WriteError($"Pack '{name}' not found in registry");
                ctx.ExitCode = 1;
                return;
            }

            if (json)
            {
                WriteJson("registry-info", pack);
                return;
            }

            Console.WriteLine($"Name:        {pack.Name}");
            Console.WriteLine($"Version:     {pack.Version}");
            Console.WriteLine($"Description: {pack.Description}");
            Console.WriteLine($"Author:      {pack.Author}");
            Console.WriteLine($"Repository:  {pack.Repo}");
            Console.WriteLine($"Tags:        {string.Join(", ", pack.Tags)}");
            Console.WriteLine($"Targets:     {string.Join(", ", pack.Targets)}");
        });

        return command;
    }

    private static Command CreateInstallCommand(RegistryClient registryClient, Option<bool> globalJsonOption)
    {
        var nameArgument = new Argument<string>(
            "name",
            "Pack name to install");

        var command = new Command("install", "Install a template pack as remote source")
        {
            nameArgument
        };

        command.SetHandler(async (InvocationContext ctx) =>
        {
            var name = ctx.ParseResult.GetValueForArgument(nameArgument);
            var json = ctx.ParseResult.GetValueForOption(globalJsonOption);
            JsonMode = json;

            var pack = await registryClient.GetPackAsync(name, ctx.GetCancellationToken());

            if (pack == null)
            {
                WriteError($"Pack '{name}' not found in registry");
                ctx.ExitCode = 1;
                return;
            }

            // Set the pack's repo as the remote source
            var existingConfig = RemoteConfig.Load();
            var config = existingConfig with { Source = pack.Repo };
            config.Save();

            if (json)
            {
                WriteJson("registry-install", new { pack = pack.Name, source = pack.Repo, status = "configured" });
                return;
            }

            WriteSuccess($"Configured '{pack.Name}' as remote source ({pack.Repo})");
            WriteInfo("Run 'copilot-assets init --source remote' to install assets from this pack");
        });

        return command;
    }

    private static Command CreateListCommand(RegistryClient registryClient, Option<bool> globalJsonOption)
    {
        var command = new Command("list", "List all available template packs");

        command.SetHandler(async (InvocationContext ctx) =>
        {
            var json = ctx.ParseResult.GetValueForOption(globalJsonOption);
            JsonMode = json;

            var packs = await registryClient.ListAsync(ctx.GetCancellationToken());

            if (json)
            {
                WriteJson("registry-list", packs);
                return;
            }

            if (packs.Count == 0)
            {
                WriteWarning("No packs available in the registry");
                return;
            }

            Console.WriteLine($"Available packs ({packs.Count}):\n");
            Console.WriteLine($"  {"Name",-30} {"Version",-10} {"Targets",-25} Description");
            Console.WriteLine($"  {new string('-', 30)} {new string('-', 10)} {new string('-', 25)} {new string('-', 30)}");

            foreach (var pack in packs)
            {
                var targets = string.Join(", ", pack.Targets);
                Console.WriteLine($"  {pack.Name,-30} {pack.Version,-10} {targets,-25} {pack.Description}");
            }
        });

        return command;
    }
}
