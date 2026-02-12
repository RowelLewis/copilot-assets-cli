using System.Text.RegularExpressions;

namespace DevTools.CopilotAssets.Infrastructure.Security;

/// <summary>
/// Validates and sanitizes user inputs to prevent security issues.
/// </summary>
public static class InputValidator
{
    private const int MaxRepositoryLength = 200;
    private const int MaxComponentLength = 100;
    private const int MaxBranchLength = 255;

    /// <summary>
    /// Validate repository format (owner/repo).
    /// </summary>
    public static bool IsValidRepository(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return false;

        // Basic length check
        if (source.Length > MaxRepositoryLength)
            return false;

        // Must be owner/repo format
        var parts = source.Split('/');
        if (parts.Length != 2)
            return false;

        // Validate each component
        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part) || part.Length > MaxComponentLength)
                return false;

            // No path traversal
            if (part.Contains("..") || part.Contains("\\"))
                return false;
        }

        // Ensure alphanumeric with hyphens/underscores only
        var pattern = @"^[a-zA-Z0-9_-]+/[a-zA-Z0-9_-]+$";
        return Regex.IsMatch(source, pattern);
    }

    /// <summary>
    /// Validate branch name to prevent injection attacks.
    /// </summary>
    public static bool IsValidBranch(string? branch)
    {
        if (string.IsNullOrWhiteSpace(branch))
            return false;

        // Length check
        if (branch.Length > MaxBranchLength)
            return false;

        // No path separators or traversal
        if (branch.Contains("..") || branch.Contains("/") || branch.Contains("\\"))
            return false;

        // No special git refs
        if (branch.StartsWith("refs/", StringComparison.OrdinalIgnoreCase) ||
            branch.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    /// <summary>
    /// Sanitize file path from remote source to prevent path traversal.
    /// Throws SecurityException if path is invalid.
    /// </summary>
    public static string SanitizePath(string remotePath)
    {
        if (string.IsNullOrWhiteSpace(remotePath))
            throw new SecurityException("Path cannot be empty");

        var trimmed = remotePath.Trim();

        // Check for absolute paths BEFORE trimming leading slashes
        if (Path.IsPathRooted(trimmed) || trimmed.StartsWith('/') || trimmed.StartsWith('\\'))
            throw new SecurityException($"Absolute paths not allowed: {remotePath}");

        // Check for path traversal attempts
        if (trimmed.Contains(".."))
            throw new SecurityException($"Path traversal detected: {remotePath}");

        // No backslashes
        if (trimmed.Contains('\\'))
            throw new SecurityException($"Invalid path separator: {remotePath}");

        // Empty after trimming
        if (string.IsNullOrEmpty(trimmed))
            throw new SecurityException("Path cannot be empty after sanitization");

        return trimmed;
    }

    /// <summary>
    /// Validate that a path stays within the .github directory.
    /// </summary>
    public static bool IsWithinGitHubDirectory(string path)
    {
        var normalized = path.Replace("\\", "/").TrimStart('/');
        return normalized.StartsWith(".github/", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals(".github", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Security exception for input validation failures.
/// </summary>
public class SecurityException : Exception
{
    public SecurityException(string message) : base(message) { }
}
