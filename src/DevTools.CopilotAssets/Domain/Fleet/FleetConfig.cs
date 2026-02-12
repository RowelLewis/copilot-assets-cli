using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevTools.CopilotAssets.Domain.Fleet;

/// <summary>
/// Configuration for managing multiple repositories.
/// </summary>
public sealed class FleetConfig
{
    [JsonPropertyName("version")]
    public int Version { get; init; } = 1;

    [JsonPropertyName("repos")]
    public List<FleetRepo> Repos { get; init; } = [];

    [JsonPropertyName("defaults")]
    public FleetDefaults Defaults { get; init; } = new();

    /// <summary>
    /// Path to the fleet config file.
    /// </summary>
    public static string GetConfigPath()
    {
        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config",
            "copilot-assets");
        return Path.Combine(configDir, "fleet.json");
    }

    /// <summary>
    /// Load fleet config from disk.
    /// </summary>
    public static FleetConfig Load()
    {
        var path = GetConfigPath();
        if (!File.Exists(path))
            return new FleetConfig();

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<FleetConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new FleetConfig();
        }
        catch
        {
            return new FleetConfig();
        }
    }

    /// <summary>
    /// Save fleet config to disk.
    /// </summary>
    public void Save()
    {
        var path = GetConfigPath();
        var dir = Path.GetDirectoryName(path)!;

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(path, JsonSerializer.Serialize(this, options));
    }
}

/// <summary>
/// Default settings for fleet repos.
/// </summary>
public sealed class FleetDefaults
{
    [JsonPropertyName("source")]
    public string Source { get; init; } = "default";

    [JsonPropertyName("targets")]
    public List<string> Targets { get; init; } = ["copilot"];

    [JsonPropertyName("branch")]
    public string Branch { get; init; } = "main";
}
