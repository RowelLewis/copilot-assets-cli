using System.Text.Json.Serialization;

namespace DevTools.CopilotAssets.Domain.Registry;

/// <summary>
/// Registry index containing all available template packs.
/// </summary>
public sealed class RegistryIndex
{
    [JsonPropertyName("version")]
    public int Version { get; init; } = 1;

    [JsonPropertyName("packs")]
    public List<PackMetadata> Packs { get; init; } = [];
}
