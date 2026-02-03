using System.Text.Json.Serialization;

namespace DevTools.CopilotAssets.Domain;

/// <summary>
/// Summary statistics for assets.
/// </summary>
public sealed record AssetSummary(
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("valid")] int Valid,
    [property: JsonPropertyName("modified")] int Modified,
    [property: JsonPropertyName("missing")] int Missing);
