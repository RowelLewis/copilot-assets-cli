using DevTools.CopilotAssets.Domain;

namespace DevTools.CopilotAssets.Infrastructure.Security;

/// <summary>
/// Validates manifest files to prevent security issues.
/// </summary>
public static class ManifestValidator
{
    /// <summary>
    /// Validate a manifest before using it.
    /// </summary>
    /// <param name="manifest">The manifest to validate.</param>
    /// <exception cref="SecurityException">If validation fails.</exception>
    public static void Validate(Manifest manifest)
    {
        // Check asset count (prevent DoS)
        if (manifest.Assets.Count > ContentLimits.MaxAssetCount)
        {
            throw new SecurityException(
                $"Too many assets in manifest: {manifest.Assets.Count} (max: {ContentLimits.MaxAssetCount})");
        }

        // Validate each asset path
        foreach (var assetPath in manifest.Assets)
        {
            try
            {
                // Multi-target tracking paths use "target:path" format
                var pathToValidate = assetPath;
                if (IsMultiTargetPath(assetPath))
                {
                    pathToValidate = assetPath[(assetPath.IndexOf(':') + 1)..];
                }

                // Sanitize path - will throw if invalid (path traversal, absolute, etc.)
                InputValidator.SanitizePath(pathToValidate);
            }
            catch (SecurityException)
            {
                throw; // Re-throw security exceptions
            }
            catch (Exception ex)
            {
                throw new SecurityException($"Invalid asset path '{assetPath}': {ex.Message}");
            }
        }

        // Validate checksums format
        foreach (var (path, checksum) in manifest.Checksums)
        {
            if (string.IsNullOrWhiteSpace(checksum))
            {
                throw new SecurityException($"Empty checksum for asset: {path}");
            }

            // SHA256 should be 64 hex characters
            if (checksum.Length != 64 || !IsHexString(checksum))
            {
                throw new SecurityException($"Invalid checksum format for asset '{path}': {checksum}");
            }
        }
    }

    /// <summary>
    /// Check if the path is a multi-target tracking path (e.g., "claude:CLAUDE.md").
    /// </summary>
    private static bool IsMultiTargetPath(string path)
    {
        var colonIdx = path.IndexOf(':');
        if (colonIdx <= 0) return false;

        var prefix = path[..colonIdx];
        var knownTargets = new[] { "copilot", "claude", "cursor", "windsurf", "cline", "aider" };
        return knownTargets.Contains(prefix, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsHexString(string str)
    {
        return str.All(c => (c >= '0' && c <= '9') ||
                            (c >= 'a' && c <= 'f') ||
                            (c >= 'A' && c <= 'F'));
    }
}
