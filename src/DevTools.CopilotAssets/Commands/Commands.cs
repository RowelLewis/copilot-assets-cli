using System.CommandLine;
using DevTools.CopilotAssets.Services;

namespace DevTools.CopilotAssets.Commands;

/// <summary>
/// Base class for all commands.
/// </summary>
public abstract class BaseCommand
{
    protected static void WriteSuccess(string message) =>
        Console.WriteLine($"✓ {message}");

    protected static void WriteError(string message) =>
        Console.Error.WriteLine($"✗ {message}");

    protected static void WriteWarning(string message) =>
        Console.WriteLine($"⚠ {message}");

    protected static void WriteInfo(string message) =>
        Console.WriteLine($"  {message}");
}

/// <summary>
/// Initialize project with Copilot assets.
/// </summary>
public sealed class InitCommand : BaseCommand
{
    public static Command Create(IPolicyAppService policyService)
    {
        var forceOption = new Option<bool>(
            ["--force", "-f"],
            "Overwrite existing files without prompting");

        var noGitOption = new Option<bool>(
            "--no-git",
            "Skip git operations (stage/commit)");

        var pathArgument = new Argument<string>(
            "path",
            () => ".",
            "Target directory (defaults to current directory)");

        var command = new Command("init", "Initialize project with Copilot assets")
        {
            forceOption,
            noGitOption,
            pathArgument
        };

        command.SetHandler(async (force, noGit, path) =>
        {
            var options = new InitOptions
            {
                TargetDirectory = path,
                Force = force,
                NoGit = noGit
            };

            var result = await policyService.InitAsync(options);

            foreach (var error in result.Errors)
                WriteError(error);
            foreach (var warning in result.Warnings)
                WriteWarning(warning);
            foreach (var info in result.Info)
                WriteInfo(info);

            Environment.ExitCode = result.IsCompliant ? 0 : 1;

        }, forceOption, noGitOption, pathArgument);

        return command;
    }
}

/// <summary>
/// Update existing assets to latest version.
/// </summary>
public sealed class UpdateCommand : BaseCommand
{
    public static Command Create(IPolicyAppService policyService)
    {
        var forceOption = new Option<bool>(
            ["--force", "-f"],
            "Force update even if already at latest version");

        var noGitOption = new Option<bool>(
            "--no-git",
            "Skip git operations");

        var pathArgument = new Argument<string>(
            "path",
            () => ".",
            "Target directory");

        var command = new Command("update", "Update assets to latest version")
        {
            forceOption,
            noGitOption,
            pathArgument
        };

        command.SetHandler(async (force, noGit, path) =>
        {
            var options = new UpdateOptions
            {
                TargetDirectory = path,
                Force = force,
                NoGit = noGit
            };

            var result = await policyService.UpdateAsync(options);

            foreach (var error in result.Errors)
                WriteError(error);
            foreach (var warning in result.Warnings)
                WriteWarning(warning);
            foreach (var info in result.Info)
                WriteInfo(info);

            Environment.ExitCode = result.IsCompliant ? 0 : 1;

        }, forceOption, noGitOption, pathArgument);

        return command;
    }
}

/// <summary>
/// Validate project compliance.
/// </summary>
public sealed class ValidateCommand : BaseCommand
{
    public static Command Create(IPolicyAppService policyService)
    {
        var ciOption = new Option<bool>(
            "--ci",
            "CI mode: JSON output and strict exit codes");

        var pathArgument = new Argument<string>(
            "path",
            () => ".",
            "Target directory");

        var command = new Command("validate", "Validate project compliance with policy")
        {
            ciOption,
            pathArgument
        };

        command.SetHandler(async (ci, path) =>
        {
            var options = new ValidateOptions
            {
                TargetDirectory = path,
                CiMode = ci
            };

            var result = await policyService.ValidateAsync(options);

            if (ci)
            {
                // JSON output for CI
                var json = System.Text.Json.JsonSerializer.Serialize(new
                {
                    compliant = result.IsCompliant,
                    errors = result.Errors,
                    warnings = result.Warnings
                }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine(json);
            }
            else
            {
                foreach (var error in result.Errors)
                    WriteError(error);
                foreach (var warning in result.Warnings)
                    WriteWarning(warning);
                foreach (var info in result.Info)
                    WriteInfo(info);

                if (result.IsCompliant)
                {
                    WriteSuccess("Validation passed");
                }
            }

            Environment.ExitCode = result.IsCompliant ? 0 : 1;

        }, ciOption, pathArgument);

        return command;
    }
}

/// <summary>
/// Run environment diagnostics.
/// </summary>
public sealed class DoctorCommand : BaseCommand
{
    public static Command Create(IPolicyAppService policyService)
    {
        var command = new Command("doctor", "Check environment and diagnose issues");

        command.SetHandler(async () =>
        {
            var result = await policyService.DiagnoseAsync();

            Console.WriteLine("Copilot Assets CLI Diagnostics");
            Console.WriteLine("==============================");
            Console.WriteLine();
            Console.WriteLine($"Tool Version:      {result.ToolVersion}");
            Console.WriteLine($"Git Available:     {(result.GitAvailable ? "✓" : "✗")}");
            Console.WriteLine($"Git Repository:    {(result.IsGitRepository ? "✓" : "✗")}");
            Console.WriteLine($"Assets Directory:  {(result.AssetsDirectoryExists ? "✓" : "✗")}");
            Console.WriteLine($"Manifest:          {(result.ManifestExists ? "✓" : "✗")}");

            if (result.InstalledVersion != null)
            {
                Console.WriteLine($"Installed Version: {result.InstalledVersion}");
            }

            Console.WriteLine();

            if (result.HasIssues)
            {
                Console.WriteLine("Issues:");
                foreach (var issue in result.Issues)
                {
                    WriteWarning(issue);
                }
                Environment.ExitCode = 1;
            }
            else
            {
                WriteSuccess("All checks passed");
            }
        });

        return command;
    }
}

/// <summary>
/// Display version information.
/// </summary>
public sealed class VersionCommand : BaseCommand
{
    public static Command Create()
    {
        var command = new Command("version", "Display version information");

        command.SetHandler(() =>
        {
            Console.WriteLine($"copilot-assets {SyncEngine.ToolVersion}");
            Console.WriteLine($"Asset version: {SyncEngine.AssetVersion}");
        });

        return command;
    }
}
