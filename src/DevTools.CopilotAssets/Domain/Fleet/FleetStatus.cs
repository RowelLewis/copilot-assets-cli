using System.Text.Json.Serialization;

namespace DevTools.CopilotAssets.Domain.Fleet;

/// <summary>
/// Compliance status for a single repository.
/// </summary>
public sealed record FleetStatus
{
    [JsonPropertyName("repo")]
    public required string Repo { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("errors")]
    public List<string> Errors { get; init; } = [];

    [JsonPropertyName("warnings")]
    public List<string> Warnings { get; init; } = [];
}

/// <summary>
/// Aggregated fleet report.
/// </summary>
public sealed class FleetReport
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("compliant")]
    public int Compliant { get; set; }

    [JsonPropertyName("nonCompliant")]
    public int NonCompliant { get; set; }

    [JsonPropertyName("unreachable")]
    public int Unreachable { get; set; }

    [JsonPropertyName("repos")]
    public List<FleetStatus> Repos { get; init; } = [];
}
