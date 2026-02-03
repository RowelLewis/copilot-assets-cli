namespace DevTools.CopilotAssets.Services.Templates;

/// <summary>
/// Provides template files from a source (default or remote).
/// </summary>
public interface ITemplateProvider
{
    /// <summary>
    /// Get templates from this provider.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing templates and source information.</returns>
    Task<TemplateResult> GetTemplatesAsync(CancellationToken ct = default);

    /// <summary>
    /// Check if this provider is available and can serve templates.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if provider is available.</returns>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);
}
