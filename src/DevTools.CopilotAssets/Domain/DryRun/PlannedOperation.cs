using System.Text.Json.Serialization;

namespace DevTools.CopilotAssets.Domain;

/// <summary>
/// A planned operation during dry-run mode.
/// </summary>
public sealed record PlannedOperation(
    [property: JsonPropertyName("type")] OperationType Type,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("reason")] string? Reason = null);
