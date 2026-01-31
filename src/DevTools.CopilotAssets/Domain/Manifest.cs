using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevTools.CopilotAssets.Domain;

/// <summary>
/// Local manifest file (.github/.copilot-assets.json) tracking installed assets.
/// </summary>
public sealed class Manifest
{
    /// <summary>
    /// Manifest file name.
    /// </summary>
    public const string FileName = ".copilot-assets.json";

    /// <summary>
    /// Path relative to project root.
    /// </summary>
    public const string RelativePath = ".github/.copilot-assets.json";

    /// <summary>
    /// Version of the installed assets.
    /// </summary>
    [JsonPropertyName("version")]
    public required string Version { get; set; }

    /// <summary>
    /// When the assets were installed/updated.
    /// </summary>
    [JsonPropertyName("installedAt")]
    public required DateTime InstalledAt { get; set; }

    /// <summary>
    /// Version of the CLI tool that installed the assets.
    /// </summary>
    [JsonPropertyName("toolVersion")]
    public required string ToolVersion { get; set; }

    /// <summary>
    /// List of installed asset paths relative to .github folder.
    /// </summary>
    [JsonPropertyName("assets")]
    public List<string> Assets { get; set; } = [];

    /// <summary>
    /// SHA256 checksums for each asset.
    /// </summary>
    [JsonPropertyName("checksums")]
    public Dictionary<string, string> Checksums { get; set; } = [];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Serialize manifest to JSON.
    /// </summary>
    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    /// <summary>
    /// Deserialize manifest from JSON.
    /// </summary>
    public static Manifest? FromJson(string json) =>
        JsonSerializer.Deserialize<Manifest>(json, JsonOptions);

    /// <summary>
    /// Create a new manifest with current timestamp.
    /// </summary>
    public static Manifest Create(string version, string toolVersion) => new()
    {
        Version = version,
        ToolVersion = toolVersion,
        InstalledAt = DateTime.UtcNow
    };
}
