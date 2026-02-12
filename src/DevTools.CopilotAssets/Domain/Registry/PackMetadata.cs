using System.Text.Json.Serialization;

namespace DevTools.CopilotAssets.Domain.Registry;

/// <summary>
/// Metadata for a template pack in the registry.
/// </summary>
public sealed class PackMetadata
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; init; } = string.Empty;

    [JsonPropertyName("repo")]
    public required string Repo { get; init; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; init; } = [];

    [JsonPropertyName("targets")]
    public List<string> Targets { get; init; } = ["copilot"];

    [JsonPropertyName("version")]
    public string Version { get; init; } = "1.0.0";
}
