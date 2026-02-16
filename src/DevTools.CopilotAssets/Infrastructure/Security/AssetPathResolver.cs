namespace DevTools.CopilotAssets.Infrastructure.Security;

/// <summary>
/// Resolves manifest tracking paths to actual file system paths relative to the project root.
/// </summary>
public static class AssetPathResolver
{
    private static readonly string[] KnownTargets = ["copilot", "claude", "cursor", "windsurf", "cline", "aider"];

    /// <summary>
    /// Resolve a manifest tracking path to a file system path relative to the project root.
    /// Multi-target paths (e.g., "claude:CLAUDE.md") return the path portion.
    /// Copilot-only paths (no prefix) return ".github/{path}".
    /// </summary>
    public static string ResolveToFileSystemPath(string trackingPath)
    {
        if (IsMultiTargetPath(trackingPath))
        {
            // "claude:CLAUDE.md" -> "CLAUDE.md"
            // "cursor:.cursor/rules/instructions.mdc" -> ".cursor/rules/instructions.mdc"
            return trackingPath[(trackingPath.IndexOf(':') + 1)..];
        }

        // Copilot-only path: relative to .github/
        return Path.Combine(".github", trackingPath);
    }

    /// <summary>
    /// Check if the path is a multi-target tracking path (e.g., "claude:CLAUDE.md").
    /// </summary>
    public static bool IsMultiTargetPath(string path)
    {
        var colonIdx = path.IndexOf(':');
        if (colonIdx <= 0) return false;

        var prefix = path[..colonIdx];
        return KnownTargets.Contains(prefix, StringComparer.OrdinalIgnoreCase);
    }
}
