namespace DevTools.CopilotAssets.Infrastructure.Security;

/// <summary>
/// Content security limits to prevent DoS attacks.
/// </summary>
public static class ContentLimits
{
    /// <summary>
    /// Maximum size for a single file (1 MB).
    /// </summary>
    public const long MaxFileSize = 1_048_576;

    /// <summary>
    /// Maximum number of assets in a manifest (prevent DoS).
    /// </summary>
    public const int MaxAssetCount = 100;

    /// <summary>
    /// Maximum manifest file size (100 KB).
    /// </summary>
    public const int MaxManifestSize = 102_400;

    /// <summary>
    /// Allowed file extensions for templates.
    /// </summary>
    public static readonly string[] AllowedExtensions =
    [
        ".md",
        ".txt",
        ".json",
        ".yaml",
        ".yml"
    ];

    /// <summary>
    /// Check if file extension is allowed.
    /// </summary>
    public static bool IsAllowedExtension(string filename)
    {
        var ext = Path.GetExtension(filename).ToLowerInvariant();
        return AllowedExtensions.Contains(ext) || string.IsNullOrEmpty(ext);
    }
}
