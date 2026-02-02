using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevTools.CopilotAssets.Infrastructure;

/// <summary>
/// Infrastructure for producing consistent JSON output from all commands.
/// </summary>
public static class JsonOutput
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Serialize a command result to JSON string.
    /// </summary>
    public static string Serialize<T>(CommandResult<T> result) =>
        JsonSerializer.Serialize(result, Options);

    /// <summary>
    /// Create a command result envelope.
    /// </summary>
    public static CommandResult<T> CreateResult<T>(
        string command,
        T result,
        int exitCode = 0,
        IReadOnlyList<string>? errors = null,
        IReadOnlyList<string>? warnings = null)
    {
        return new CommandResult<T>(
            Command: command,
            Version: GetToolVersion(),
            Timestamp: DateTime.UtcNow,
            Success: exitCode == 0,
            ExitCode: exitCode,
            Result: result,
            Errors: errors ?? [],
            Warnings: warnings ?? []);
    }

    private static string GetToolVersion() =>
        typeof(JsonOutput).Assembly.GetName().Version?.ToString() ?? "1.1.0";
}
