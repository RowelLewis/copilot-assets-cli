using System.CommandLine;
using System.CommandLine.Invocation;
using DevTools.CopilotAssets.Domain.Configuration;

namespace DevTools.CopilotAssets.Commands;

/// <summary>
/// Configuration management command with subcommands: get, set, list, reset.
/// </summary>
public sealed class ConfigCommand : BaseCommand
{
    public static Command Create(Option<bool> globalJsonOption)
    {
        var command = new Command("config", "Manage remote template source configuration");

        command.AddCommand(CreateGetCommand(globalJsonOption));
        command.AddCommand(CreateSetCommand(globalJsonOption));
        command.AddCommand(CreateListCommand(globalJsonOption));
        command.AddCommand(CreateResetCommand(globalJsonOption));

        return command;
    }

    private static Command CreateGetCommand(Option<bool> globalJsonOption)
    {
        var keyArgument = new Argument<string>(
            "key",
            "Configuration key to get (source, branch)");

        var command = new Command("get", "Get a configuration value")
        {
            keyArgument
        };

        command.SetHandler((InvocationContext ctx) =>
        {
            var key = ctx.ParseResult.GetValueForArgument(keyArgument);
            var json = ctx.ParseResult.GetValueForOption(globalJsonOption);
            JsonMode = json;

            var config = RemoteConfig.Load();

            var value = key.ToLowerInvariant() switch
            {
                "source" => config.Source ?? "(not set)",
                "branch" => config.Branch,
                _ => null
            };

            if (value == null)
            {
                if (json)
                    WriteJson("config get", new { key, value = (string?)null }, 1, [$"Unknown key: {key}. Valid keys: source, branch"]);
                else
                    WriteError($"Unknown key: {key}. Valid keys: source, branch");
                Environment.ExitCode = 1;
                return;
            }

            if (json)
                WriteJson("config get", new { key, value });
            else
                Console.WriteLine(value);
        });

        return command;
    }

    private static Command CreateSetCommand(Option<bool> globalJsonOption)
    {
        var keyArgument = new Argument<string>(
            "key",
            "Configuration key to set (source, branch)");

        var valueArgument = new Argument<string>(
            "value",
            "Value to set");

        var command = new Command("set", "Set a configuration value")
        {
            keyArgument,
            valueArgument
        };

        command.SetHandler((InvocationContext ctx) =>
        {
            var key = ctx.ParseResult.GetValueForArgument(keyArgument);
            var value = ctx.ParseResult.GetValueForArgument(valueArgument);
            var json = ctx.ParseResult.GetValueForOption(globalJsonOption);
            JsonMode = json;

            var config = RemoteConfig.Load();
            RemoteConfig newConfig;

            switch (key.ToLowerInvariant())
            {
                case "source":
                    if (!RemoteConfig.IsValidSource(value))
                    {
                        if (json)
                            WriteJson("config set", new { key, value }, 1, ["Invalid repository format. Use \"owner/repo\""]);
                        else
                            WriteError("Invalid repository format. Use \"owner/repo\"");
                        Environment.ExitCode = 1;
                        return;
                    }
                    newConfig = config with { Source = value };
                    break;

                case "branch":
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        if (json)
                            WriteJson("config set", new { key, value }, 1, ["Branch cannot be empty"]);
                        else
                            WriteError("Branch cannot be empty");
                        Environment.ExitCode = 1;
                        return;
                    }
                    if (!RemoteConfig.IsValidBranch(value))
                    {
                        if (json)
                            WriteJson("config set", new { key, value }, 1, ["Invalid branch name. Branch names cannot contain path separators or special characters"]);
                        else
                            WriteError("Invalid branch name. Branch names cannot contain path separators or special characters");
                        Environment.ExitCode = 1;
                        return;
                    }
                    newConfig = config with { Branch = value };
                    break;

                default:
                    if (json)
                        WriteJson("config set", new { key, value }, 1, [$"Unknown key: {key}. Valid keys: source, branch"]);
                    else
                        WriteError($"Unknown key: {key}. Valid keys: source, branch");
                    Environment.ExitCode = 1;
                    return;
            }

            newConfig.Save();

            if (json)
                WriteJson("config set", new { key, value, saved = true });
            else
                WriteSuccess($"Set {key} = {value}");
        });

        return command;
    }

    private static Command CreateListCommand(Option<bool> globalJsonOption)
    {
        var command = new Command("list", "List all configuration values");

        command.SetHandler((InvocationContext ctx) =>
        {
            var json = ctx.ParseResult.GetValueForOption(globalJsonOption);
            JsonMode = json;

            var config = RemoteConfig.Load();

            if (json)
            {
                WriteJson("config list", new
                {
                    source = config.Source,
                    branch = config.Branch,
                    hasRemoteSource = config.HasRemoteSource
                });
            }
            else
            {
                Console.WriteLine("Current configuration:");
                Console.WriteLine($"  source = {config.Source ?? "(not set)"}");
                Console.WriteLine($"  branch = {config.Branch}");
                Console.WriteLine();

                if (config.HasRemoteSource)
                {
                    WriteInfo($"Templates will be fetched from: https://github.com/{config.Source}/tree/{config.Branch}/.github");
                }
                else
                {
                    WriteInfo("Using bundled templates (no remote source configured)");
                }
            }
        });

        return command;
    }

    private static Command CreateResetCommand(Option<bool> globalJsonOption)
    {
        var command = new Command("reset", "Reset configuration to defaults (use bundled templates)");

        command.SetHandler((InvocationContext ctx) =>
        {
            var json = ctx.ParseResult.GetValueForOption(globalJsonOption);
            JsonMode = json;

            RemoteConfig.Reset();

            if (json)
                WriteJson("config reset", new { reset = true });
            else
                WriteSuccess("Configuration reset to defaults. Using bundled templates.");
        });

        return command;
    }
}
