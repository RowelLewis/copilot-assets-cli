using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using DevTools.CopilotAssets.Commands;
using DevTools.CopilotAssets.Domain.Configuration;
using DevTools.CopilotAssets.Services;
using DevTools.CopilotAssets.Services.Adapters;
using DevTools.CopilotAssets.Services.Fleet;
using DevTools.CopilotAssets.Services.Http;
using DevTools.CopilotAssets.Services.Registry;
using DevTools.CopilotAssets.Services.Templates;

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

        // Configuration
        services.AddSingleton(_ => RemoteConfig.Load());

        // Template providers and factory
        services.AddSingleton<GitHubClient>();
        services.AddSingleton<BundledTemplateProvider>();
        services.AddSingleton<TemplateProviderFactory>();

        // Output adapters
        services.AddSingleton<OutputAdapterFactory>();

        // Registry
        services.AddSingleton<RegistryClient>();

        // Fleet management
        services.AddSingleton<FleetManager>();
        services.AddSingleton<FleetSyncService>();

        // Application services - use factory for dynamic source selection
        services.AddSingleton<SyncEngine>(sp => new SyncEngine(
            sp.GetRequiredService<IFileSystemService>(),
            sp.GetRequiredService<IGitService>(),
            sp.GetRequiredService<TemplateProviderFactory>()));
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
        var registryClient = services.GetRequiredService<RegistryClient>();
        var fleetManager = services.GetRequiredService<FleetManager>();
        var fleetSyncService = services.GetRequiredService<FleetSyncService>();

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
            ConfigCommand.Create(jsonOption),
            RegistryCommand.Create(registryClient, jsonOption),
            FleetCommand.Create(fleetManager, fleetSyncService, jsonOption),
            VersionCommand.Create(jsonOption)
        };

        // Add global option
        rootCommand.AddGlobalOption(jsonOption);

        return rootCommand;
    }
}
