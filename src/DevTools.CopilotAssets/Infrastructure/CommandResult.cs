using System.Text.Json.Serialization;

namespace DevTools.CopilotAssets.Infrastructure;

/// <summary>
/// Common envelope for all JSON command output.
/// </summary>
/// <typeparam name="T">The type of the result payload.</typeparam>
public sealed record CommandResult<T>(
    [property: JsonPropertyName("command")] string Command,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp,
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("exitCode")] int ExitCode,
    [property: JsonPropertyName("result")] T Result,
    [property: JsonPropertyName("errors")] IReadOnlyList<string> Errors,
    [property: JsonPropertyName("warnings")] IReadOnlyList<string> Warnings);
