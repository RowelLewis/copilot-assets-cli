using DevTools.CopilotAssets.Infrastructure;
using DevTools.CopilotAssets.Infrastructure.Security;

namespace DevTools.CopilotAssets.Commands;

/// <summary>
/// Base class for all commands.
/// </summary>
public abstract class BaseCommand
{
    protected static bool JsonMode { get; set; }

    protected static void WriteSuccess(string message)
    {
        if (!JsonMode)
            Console.WriteLine($"✓ {message}");
    }

    protected static void WriteError(string message)
    {
        if (!JsonMode)
        {
            // Redact sensitive data from error messages
            var safe = TokenRedactor.RedactSensitiveData(message);
            safe = TokenRedactor.RedactPaths(safe);
            Console.Error.WriteLine($"✗ {safe}");
        }
    }

    protected static void WriteException(Exception ex)
    {
        if (!JsonMode)
        {
            // Sanitize exception before displaying
            var safe = TokenRedactor.SanitizeException(ex);
            Console.Error.WriteLine($"✗ {safe}");
        }
    }

    protected static void WriteWarning(string message)
    {
        if (!JsonMode)
            Console.WriteLine($"⚠ {message}");
    }

    protected static void WriteInfo(string message)
    {
        if (!JsonMode)
            Console.WriteLine($"  {message}");
    }

    protected static void WriteJson<T>(string command, T result, int exitCode = 0,
        IReadOnlyList<string>? errors = null, IReadOnlyList<string>? warnings = null)
    {
        var cmdResult = JsonOutput.CreateResult(command, result, exitCode, errors, warnings);
        Console.WriteLine(JsonOutput.Serialize(cmdResult));
    }
}
