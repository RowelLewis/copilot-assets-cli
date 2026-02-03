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
                // Sanitize path - will throw if invalid
                var sanitized = InputValidator.SanitizePath(assetPath);

                // Ensure path is within .github directory (allow both with and without prefix)
                var normalized = sanitized.Replace("\\", "/");
                if (!InputValidator.IsWithinGitHubDirectory($".github/{normalized}") &&
                    !InputValidator.IsWithinGitHubDirectory(normalized))
                {
                    throw new SecurityException(
                        $"Asset path must be within .github directory: {assetPath}");
                }
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

    private static bool IsHexString(string str)
    {
        return str.All(c => (c >= '0' && c <= '9') ||
                            (c >= 'a' && c <= 'f') ||
                            (c >= 'A' && c <= 'F'));
    }
}
