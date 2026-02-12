using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevTools.CopilotAssets.Domain;

/// <summary>
/// Local manifest file (.github/.copilot-assets.json) tracking installed assets.
/// Schema v2 - uses checksums for change detection instead of fake version numbers.
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
    /// Current schema version for the manifest format.
    /// </summary>
    public const int CurrentSchemaVersion = 3;

    /// <summary>
    /// Schema version for manifest format compatibility.
    /// </summary>
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

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
    /// Target tools that assets were generated for.
    /// </summary>
    [JsonPropertyName("targets")]
    public List<string> Targets { get; set; } = ["copilot"];

    /// <summary>
    /// Source of the templates (default or remote).
    /// </summary>
    [JsonPropertyName("source")]
    public TemplateSource Source { get; set; } = TemplateSource.Default();

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
    public static Manifest Create(string toolVersion, TemplateSource? source = null, IEnumerable<TargetTool>? targets = null) => new()
    {
        SchemaVersion = CurrentSchemaVersion,
        ToolVersion = toolVersion,
        InstalledAt = DateTime.UtcNow,
        Source = source ?? TemplateSource.Default(),
        Targets = targets?.Select(t => t.ToConfigName()).ToList() ?? ["copilot"]
    };
}
