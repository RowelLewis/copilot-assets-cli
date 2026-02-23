using System.Text.Json.Serialization;

namespace DevTools.CopilotAssets.Domain;

/// <summary>
/// Result of listing assets.
/// </summary>
public sealed record AssetListResult(
    [property: JsonPropertyName("projectPath")] string ProjectPath,
    [property: JsonPropertyName("assets")] IReadOnlyList<AssetInfo> Assets,
    [property: JsonPropertyName("summary")] AssetSummary Summary,
    [property: JsonPropertyName("source")] TemplateSource? Source = null,
    [property: JsonPropertyName("targets")] IReadOnlyList<string>? Targets = null);
