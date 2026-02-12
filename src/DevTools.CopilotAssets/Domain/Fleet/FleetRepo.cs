using System.Text.Json.Serialization;

namespace DevTools.CopilotAssets.Domain.Fleet;

/// <summary>
/// Configuration for a single repository in the fleet.
/// </summary>
public sealed class FleetRepo
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("source")]
    public string? Source { get; init; }

    [JsonPropertyName("targets")]
    public List<string>? Targets { get; init; }

    [JsonPropertyName("branch")]
    public string? Branch { get; init; }
}
