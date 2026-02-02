using System.Text.Json.Serialization;

namespace DevTools.CopilotAssets.Domain;

/// <summary>
/// Summary of dry-run operations.
/// </summary>
public sealed record DryRunSummary(
    [property: JsonPropertyName("creates")] int Creates,
    [property: JsonPropertyName("updates")] int Updates,
    [property: JsonPropertyName("deletes")] int Deletes,
    [property: JsonPropertyName("skips")] int Skips,
    [property: JsonPropertyName("modifies")] int Modifies);
