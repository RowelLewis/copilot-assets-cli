using DevTools.CopilotAssets.Domain.Configuration;

namespace DevTools.CopilotAssets.Commands;

/// <summary>
/// Result of source selection.
/// </summary>
public record SourceSelectionResult(
    bool UseDefault,
    string? SourceOverride = null,
    bool Cancelled = false);

/// <summary>
/// Shared source selection logic for init and update commands.
/// </summary>
public static class SourceSelector
{
    /// <summary>
    /// Prompt user to select template source interactively.
    /// </summary>
    /// <returns>Selection result with source information</returns>
    public static SourceSelectionResult PromptForSource()
    {
        var config = RemoteConfig.Load();
        var hasConfiguredRemote = config.HasRemoteSource;

        // Build menu options
        var options = new List<string> { "Default templates (included with tool)" };

        if (hasConfiguredRemote)
        {
            options.Add($"Remote: {config.Source}@{config.Branch}");
            options.Add("Different remote repository...");
        }
        else
        {
            options.Add("Remote repository (GitHub)...");
        }

        // Show interactive menu
        var selection = InteractiveMenu.Show("Select template source:", options);

        // Handle cancellation
        if (selection == -1)
        {
            return new SourceSelectionResult(UseDefault: true, Cancelled: true);
        }

        // Option 1: Default templates
        if (selection == 0)
        {
            Console.WriteLine("Using default templates");
            return new SourceSelectionResult(UseDefault: true);
        }

        // Option 2 with configured remote: Use saved config
        if (hasConfiguredRemote && selection == 1)
        {
            Console.WriteLine($"Using remote: {config.Source}@{config.Branch}");
            return new SourceSelectionResult(
                UseDefault: false,
                SourceOverride: $"{config.Source}@{config.Branch}");
        }

        // Option 2 without config OR Option 3 with config: Enter/edit remote
        return PromptForRemoteDetails(config, hasConfiguredRemote);
    }

    private static SourceSelectionResult PromptForRemoteDetails(RemoteConfig config, bool hasConfiguredRemote)
    {
        Console.WriteLine();

        // Prompt for repository
        var defaultRepo = hasConfiguredRemote ? config.Source : null;
        var repo = InteractiveMenu.PromptWithDefault("Repository (owner/repo)", defaultRepo);

        if (string.IsNullOrEmpty(repo))
        {
            Console.WriteLine("No repository specified. Using default templates.");
            return new SourceSelectionResult(UseDefault: true);
        }

        if (!repo.Contains('/'))
        {
            Console.WriteLine($"Invalid repository format: '{repo}'. Expected 'owner/repo'.");
            Console.WriteLine("Using default templates.");
            return new SourceSelectionResult(UseDefault: true);
        }

        // Prompt for branch
        var defaultBranch = hasConfiguredRemote ? config.Branch : "main";
        var branch = InteractiveMenu.PromptWithDefault("Branch", defaultBranch);

        if (string.IsNullOrEmpty(branch))
        {
            branch = "main";
        }

        var sourceOverride = $"{repo}@{branch}";

        // Offer to save if different from current config
        var currentSource = hasConfiguredRemote ? $"{config.Source}@{config.Branch}" : null;
        if (sourceOverride != currentSource)
        {
            Console.WriteLine();
            if (InteractiveMenu.Confirm("Save as default remote?"))
            {
                var newConfig = config with { Source = repo, Branch = branch };
                newConfig.Save();
                Console.WriteLine("✓ Configuration saved");
            }
        }

        Console.WriteLine($"Using remote: {sourceOverride}");
        return new SourceSelectionResult(UseDefault: false, SourceOverride: sourceOverride);
    }

    /// <summary>
    /// Parse source from command line argument.
    /// </summary>
    public static SourceSelectionResult ParseSourceArgument(string source)
    {
        if (source.Equals("default", StringComparison.OrdinalIgnoreCase))
        {
            return new SourceSelectionResult(UseDefault: true);
        }

        if (!source.Contains('/'))
        {
            return new SourceSelectionResult(UseDefault: true, Cancelled: true);
        }

        return new SourceSelectionResult(UseDefault: false, SourceOverride: source);
    }

    /// <summary>
    /// Get source selection - either from CLI argument or interactive prompt.
    /// </summary>
    public static (SourceSelectionResult Result, string? Error) GetSourceSelection(
        string? sourceArg,
        bool jsonMode)
    {
        // If explicit source provided via CLI
        if (!string.IsNullOrEmpty(sourceArg))
        {
            if (sourceArg.Equals("default", StringComparison.OrdinalIgnoreCase))
            {
                return (new SourceSelectionResult(UseDefault: true), null);
            }

            if (!sourceArg.Contains('/'))
            {
                return (new SourceSelectionResult(UseDefault: true, Cancelled: true),
                    $"Invalid source: '{sourceArg}'. Use 'default' or 'owner/repo[@branch]'.");
            }

            return (new SourceSelectionResult(UseDefault: false, SourceOverride: sourceArg), null);
        }

        // JSON mode - no interactive prompts, use default
        if (jsonMode)
        {
            return (new SourceSelectionResult(UseDefault: true), null);
        }

        // Interactive mode
        try
        {
            var result = PromptForSource();
            return (result, null);
        }
        catch (IOException)
        {
            // Input not available - fall back to default
            return (new SourceSelectionResult(UseDefault: true), null);
        }
    }
}
