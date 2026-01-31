using System.Text.Json.Serialization;

namespace DevTools.CopilotAssets.Domain;

/// <summary>
/// Summary of verification results.
/// </summary>
public sealed record VerifySummary(
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("valid")] int Valid,
    [property: JsonPropertyName("modified")] int Modified,
    [property: JsonPropertyName("missing")] int Missing,
    [property: JsonPropertyName("restored")] int Restored);
