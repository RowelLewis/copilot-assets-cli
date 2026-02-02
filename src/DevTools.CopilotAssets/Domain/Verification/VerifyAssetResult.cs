using System.Text.Json.Serialization;

namespace DevTools.CopilotAssets.Domain;

/// <summary>
/// Verification result for a single asset.
/// </summary>
public sealed record VerifyAssetResult(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("status")] VerifyStatus Status,
    [property: JsonPropertyName("expectedChecksum")] string? ExpectedChecksum = null,
    [property: JsonPropertyName("actualChecksum")] string? ActualChecksum = null);
