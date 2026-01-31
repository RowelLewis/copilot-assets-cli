using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using DevTools.CopilotAssets.Commands;
using DevTools.CopilotAssets.Services;

namespace DevTools.CopilotAssets;

/// <summary>
/// Copilot Assets CLI - GitHub Copilot Asset Distribution Tool
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Setup dependency injection
        var services = ConfigureServices();

        // Build root command
        var rootCommand = BuildRootCommand(services);

        // Execute
        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Configure dependency injection services.
    /// </summary>
    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Infrastructure adapters
        services.AddSingleton<IFileSystemService, FileSystemService>();
        services.AddSingleton<IGitService, GitService>();

        // Application services
        services.AddSingleton<SyncEngine>();
        services.AddSingleton<ValidationEngine>();
        services.AddSingleton<IPolicyAppService, PolicyAppService>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Build the CLI command structure.
    /// </summary>
    private static RootCommand BuildRootCommand(IServiceProvider services)
    {
        var policyService = services.GetRequiredService<IPolicyAppService>();

        // Global --json option
        var jsonOption = new Option<bool>(
            "--json",
            "Output results as JSON");

        var rootCommand = new RootCommand("Copilot Assets CLI - GitHub Copilot Asset Distribution Tool")
        {
            InitCommand.Create(policyService, jsonOption),
            UpdateCommand.Create(policyService, jsonOption),
            ValidateCommand.Create(policyService, jsonOption),
            ListCommand.Create(policyService, jsonOption),
            VerifyCommand.Create(policyService, jsonOption),
            DoctorCommand.Create(policyService, jsonOption),
            VersionCommand.Create(jsonOption)
        };

        // Add global option
        rootCommand.AddGlobalOption(jsonOption);

        return rootCommand;
    }
}
