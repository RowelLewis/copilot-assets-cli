using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevTools.CopilotAssets.Domain.Configuration;

/// <summary>
/// Configuration for remote template source.
/// </summary>
public sealed record RemoteConfig
{
    /// <summary>
    /// GitHub repository in "owner/repo" format.
    /// Null means use bundled templates only.
    /// </summary>
    [JsonPropertyName("source")]
    public string? Source { get; init; }

    /// <summary>
    /// Branch to fetch from. Defaults to "main".
    /// </summary>
    [JsonPropertyName("branch")]
    public string Branch { get; init; } = "main";

    /// <summary>
    /// Whether a remote source is configured.
    /// </summary>
    [JsonIgnore]
    public bool HasRemoteSource => !string.IsNullOrWhiteSpace(Source);

    /// <summary>
    /// Default configuration (bundled only).
    /// </summary>
    public static RemoteConfig Default => new();

    /// <summary>
    /// Path to the config file.
    /// </summary>
    public static string GetConfigPath()
    {
        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config",
            "copilot-assets");
        return Path.Combine(configDir, "config.json");
    }

    /// <summary>
    /// Load configuration from disk.
    /// </summary>
    public static RemoteConfig Load()
    {
        var path = GetConfigPath();
        if (!File.Exists(path))
        {
            return Default;
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<RemoteConfig>(json) ?? Default;
        }
        catch
        {
            return Default;
        }
    }

    /// <summary>
    /// Save configuration to disk.
    /// </summary>
    public void Save()
    {
        var path = GetConfigPath();
        var dir = Path.GetDirectoryName(path)!;

        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(this, options);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Delete the configuration file (reset to default).
    /// </summary>
    public static void Reset()
    {
        var path = GetConfigPath();
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Validate the source format (owner/repo).
    /// </summary>
    public static bool IsValidSource(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return false;

        var parts = source.Split('/');
        return parts.Length == 2
            && !string.IsNullOrWhiteSpace(parts[0])
            && !string.IsNullOrWhiteSpace(parts[1]);
    }
}
