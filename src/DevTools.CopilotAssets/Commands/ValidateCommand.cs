using System.CommandLine;
using System.CommandLine.Invocation;
using DevTools.CopilotAssets.Services;

namespace DevTools.CopilotAssets.Commands;

/// <summary>
/// Validate project compliance.
/// </summary>
public sealed class ValidateCommand : BaseCommand
{
    public static Command Create(IPolicyAppService policyService, Option<bool> globalJsonOption)
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

        command.SetHandler(async (InvocationContext ctx) =>
        {
            var ci = ctx.ParseResult.GetValueForOption(ciOption);
            var path = ctx.ParseResult.GetValueForArgument(pathArgument);
            var json = ctx.ParseResult.GetValueForOption(globalJsonOption);
            JsonMode = json || ci;

            var options = new ValidateOptions
            {
                TargetDirectory = path,
                CiMode = ci
            };

            var result = await policyService.ValidateAsync(options);

            if (json || ci)
            {
                WriteJson("validate", new
                {
                    compliant = result.IsCompliant,
                    path = path
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

                if (result.IsCompliant)
                {
                    WriteSuccess("Validation passed");
                }
            }

            Environment.ExitCode = result.IsCompliant ? 0 : 1;
        });

        return command;
    }
}
