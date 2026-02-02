namespace DevTools.CopilotAssets.Services.Templates;

/// <summary>
/// Result from a template provider.
/// </summary>
/// <param name="Templates">List of template files.</param>
/// <param name="Source">Source description (e.g., "bundled", "remote:org/repo@main", "cached").</param>
/// <param name="FromCache">Whether templates were served from local cache.</param>
public sealed record TemplateResult(
    IReadOnlyList<TemplateFile> Templates,
    string Source,
    bool FromCache)
{
    /// <summary>
    /// Create an empty result.
    /// </summary>
    public static TemplateResult Empty(string source) => new([], source, false);

    /// <summary>
    /// Create a result indicating failure to fetch templates.
    /// </summary>
    public static TemplateResult Failed(string source, string error) =>
        new([], source, false) { Error = error };

    /// <summary>
    /// Error message if template fetching failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Whether the result contains any templates.
    /// </summary>
    public bool HasTemplates => Templates.Count > 0;

    /// <summary>
    /// Whether there was an error fetching templates.
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(Error);
}
