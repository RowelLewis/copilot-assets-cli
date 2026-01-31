using System.CommandLine;
using System.CommandLine.Invocation;
using DevTools.CopilotAssets.Services;

namespace DevTools.CopilotAssets.Commands;

/// <summary>
/// Run environment diagnostics.
/// </summary>
public sealed class DoctorCommand : BaseCommand
{
    public static Command Create(IPolicyAppService policyService, Option<bool> globalJsonOption)
    {
        var command = new Command("doctor", "Check environment and diagnose issues");

        command.SetHandler(async (InvocationContext ctx) =>
        {
            var json = ctx.ParseResult.GetValueForOption(globalJsonOption);
            JsonMode = json;

            var result = await policyService.DiagnoseAsync();

            if (json)
            {
                WriteJson("doctor", new
                {
                    environment = new
                    {
                        toolVersion = result.ToolVersion,
                        gitAvailable = result.GitAvailable,
                        isGitRepository = result.IsGitRepository,
                        assetsDirectoryExists = result.AssetsDirectoryExists,
                        manifestExists = result.ManifestExists,
                        installedVersion = result.InstalledVersion
                    },
                    issues = result.Issues
                }, result.HasIssues ? 1 : 0);
            }
            else
            {
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
                }
                else
                {
                    WriteSuccess("All checks passed");
                }
            }

            Environment.ExitCode = result.HasIssues ? 1 : 0;
        });

        return command;
    }
}
