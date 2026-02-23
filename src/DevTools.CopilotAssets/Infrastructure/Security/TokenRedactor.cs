using System.Text.RegularExpressions;

namespace DevTools.CopilotAssets.Infrastructure.Security;

/// <summary>
/// Redacts sensitive data (tokens, credentials) from strings.
/// </summary>
public static class TokenRedactor
{
    /// <summary>
    /// Redact sensitive data from error messages and logs.
    /// </summary>
    public static string RedactSensitiveData(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        // Redact GitHub personal access tokens (ghp_, gho_, ghu_, ghs_, ghr_)
        message = Regex.Replace(message, @"gh[psour]_[\w]{16,255}", "[REDACTED_TOKEN]", RegexOptions.IgnoreCase);

        // Redact Bearer tokens
        message = Regex.Replace(message, @"(Bearer\s+)[\w\-\.]+", "$1[REDACTED]", RegexOptions.IgnoreCase);

        // Redact token= patterns
        message = Regex.Replace(message, @"(token[=:]\s*)[\w\-\.]+", "$1[REDACTED]", RegexOptions.IgnoreCase);

        // Redact Authorization headers
        message = Regex.Replace(message, @"(Authorization[:\s]+)[\w\s\-\.]+", "$1[REDACTED]", RegexOptions.IgnoreCase);

        // Redact api_key= patterns
        message = Regex.Replace(message, @"(api[_-]?key[=:]\s*)[\w\-\.]+", "$1[REDACTED]", RegexOptions.IgnoreCase);

        return message;
    }

    /// <summary>
    /// Redact home directory paths from messages.
    /// </summary>
    public static string RedactPaths(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(userHome))
        {
            message = message.Replace(userHome, "~");
        }

        return message;
    }

    /// <summary>
    /// Sanitize exception message before displaying to user.
    /// </summary>
    public static string SanitizeException(Exception ex)
    {
        var message = ex.ToString();
        message = RedactSensitiveData(message);
        message = RedactPaths(message);
        return message;
    }
}
