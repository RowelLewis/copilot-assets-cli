using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Text.Json;
using DevTools.CopilotAssets.Domain.Configuration;
using DevTools.CopilotAssets.Domain.Registry;
using DevTools.CopilotAssets.Services.Registry;

namespace DevTools.CopilotAssets.Commands;

/// <summary>
/// Registry command with subcommands: search, info, install, list, publish.
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
        command.AddCommand(CreatePublishCommand(globalJsonOption));

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

    private static Command CreatePublishCommand(Option<bool> globalJsonOption)
    {
        var pathArgument = new Argument<string>(
            "path",
            () => ".",
            "Path to the pack directory containing pack.json");

        var submitOption = new Option<bool>(
            "--submit",
            "Submit the pack to the registry by creating a GitHub PR");

        var command = new Command("publish", "Publish a template pack to the community registry")
        {
            pathArgument,
            submitOption
        };

        command.SetHandler(async (InvocationContext ctx) =>
        {
            var path = ctx.ParseResult.GetValueForArgument(pathArgument);
            var submit = ctx.ParseResult.GetValueForOption(submitOption);
            var json = ctx.ParseResult.GetValueForOption(globalJsonOption);
            JsonMode = json;

            // Validate pack.json exists
            var packJsonPath = Path.Combine(path, "pack.json");
            if (!File.Exists(packJsonPath))
            {
                var error = $"pack.json not found in '{path}'. A valid pack.json is required to publish.";
                if (json)
                    WriteJson("registry-publish", new { }, 1, [error]);
                else
                    WriteError(error);
                ctx.ExitCode = 1;
                return;
            }

            // Parse and validate pack.json
            PackMetadata? pack;
            try
            {
                var content = await File.ReadAllTextAsync(packJsonPath);
                pack = JsonSerializer.Deserialize<PackMetadata>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                var error = $"Failed to parse pack.json: {ex.Message}";
                if (json)
                    WriteJson("registry-publish", new { }, 1, [error]);
                else
                    WriteError(error);
                ctx.ExitCode = 1;
                return;
            }

            if (pack == null)
            {
                var error = "pack.json is empty or invalid.";
                if (json)
                    WriteJson("registry-publish", new { }, 1, [error]);
                else
                    WriteError(error);
                ctx.ExitCode = 1;
                return;
            }

            // Validate required fields
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(pack.Name)) errors.Add("'name' is required");
            if (string.IsNullOrWhiteSpace(pack.Description)) errors.Add("'description' is required");
            if (string.IsNullOrWhiteSpace(pack.Author)) errors.Add("'author' is required");
            if (string.IsNullOrWhiteSpace(pack.Repo)) errors.Add("'repo' is required (GitHub owner/repo format)");
            if (pack.Targets.Count == 0) errors.Add("'targets' must have at least one entry");

            if (errors.Count > 0)
            {
                if (json)
                    WriteJson("registry-publish", new { pack = pack.Name }, 1, errors);
                else
                {
                    WriteError("pack.json validation failed:");
                    foreach (var e in errors)
                        WriteError($"  - {e}");
                }
                ctx.ExitCode = 1;
                return;
            }

            if (json)
            {
                WriteJson("registry-publish", new
                {
                    pack = pack.Name,
                    version = pack.Version,
                    repo = pack.Repo,
                    status = submit ? "submitted" : "validated"
                });
            }
            else
            {
                Console.WriteLine($"Pack: {pack.Name} v{pack.Version}");
                Console.WriteLine($"  Author:      {pack.Author}");
                Console.WriteLine($"  Description: {pack.Description}");
                Console.WriteLine($"  Repo:        {pack.Repo}");
                Console.WriteLine($"  Targets:     {string.Join(", ", pack.Targets)}");
                if (pack.Tags.Count > 0)
                    Console.WriteLine($"  Tags:        {string.Join(", ", pack.Tags)}");
                Console.WriteLine();
            }

            if (submit)
            {
                var registryRepo = RegistryClient.DefaultRegistryRepo;
                if (!json)
                    WriteInfo($"Submitting to {registryRepo}...");

                try
                {
                    var packJson = JsonSerializer.Serialize(pack, new JsonSerializerOptions { WriteIndented = true });
                    var prBody = $"Add pack: {pack.Name} v{pack.Version}\n\n```json\n{packJson}\n```\n\nSubmitted via `copilot-assets registry publish`.";
                    var prTitle = $"feat: add pack {pack.Name} v{pack.Version}";
                    var ghArgs = $"issue create --repo \"{registryRepo}\" --title \"{prTitle}\" --body \"{prBody.Replace("\"", "\\\"")}\"";

                    var psi = new ProcessStartInfo
                    {
                        FileName = "gh",
                        Arguments = ghArgs,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(psi);
                    if (process != null)
                    {
                        var output = await process.StandardOutput.ReadToEndAsync();
                        var stderr = await process.StandardError.ReadToEndAsync();
                        await process.WaitForExitAsync();

                        if (process.ExitCode == 0)
                        {
                            var issueUrl = output.Trim();
                            if (json)
                                WriteJson("registry-publish", new { pack = pack.Name, version = pack.Version, status = "submitted", issueUrl });
                            else
                            {
                                WriteSuccess($"Submission issue created: {issueUrl}");
                            }
                        }
                        else
                        {
                            // gh failed - fall back to manual instructions
                            if (!json)
                            {
                                WriteError($"gh CLI returned an error: {stderr.Trim()}");
                                WriteSuccess($"Pack validated. To submit manually, create an issue at:");
                                WriteInfo($"  https://github.com/{registryRepo}/issues/new");
                                WriteInfo($"  Include pack.json content in the issue body.");
                            }
                            ctx.ExitCode = 1;
                        }
                    }
                    else
                    {
                        // gh not found - fall back gracefully
                        if (!json)
                        {
                            WriteError("gh CLI not found. Install GitHub CLI to auto-submit.");
                            WriteSuccess($"Pack validated. To submit manually, create an issue at:");
                            WriteInfo($"  https://github.com/{registryRepo}/issues/new");
                            WriteInfo($"  Include pack.json content in the issue body.");
                        }
                        ctx.ExitCode = 1;
                    }
                }
                catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is FileNotFoundException)
                {
                    // gh CLI not installed
                    if (!json)
                    {
                        WriteError("gh CLI not found. Install GitHub CLI (https://cli.github.com/) to auto-submit.");
                        WriteSuccess($"Pack validated. To submit manually, create an issue at:");
                        WriteInfo($"  https://github.com/{registryRepo}/issues/new");
                        WriteInfo($"  Include pack.json content in the issue body.");
                    }
                    ctx.ExitCode = 1;
                }
                catch (Exception ex)
                {
                    WriteError($"Submission failed: {ex.Message}");
                    ctx.ExitCode = 1;
                }
            }
            else if (!json)
            {
                WriteSuccess($"Pack '{pack.Name}' validated successfully.");
                WriteInfo($"Run with --submit to publish to the registry.");
            }
        });

        return command;
    }
}
