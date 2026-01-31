using System.Text.Json.Serialization;

namespace DevTools.CopilotAssets.Domain;

/// <summary>
/// Information about an installed asset.
/// </summary>
public sealed record AssetInfo(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("valid")] bool Valid,
    [property: JsonPropertyName("checksum")] string? Checksum = null,
    [property: JsonPropertyName("reason")] string? Reason = null);
