namespace DevTools.CopilotAssets.Domain;

/// <summary>
/// Defines policy rules for Copilot asset compliance validation.
/// </summary>
public sealed record PolicyDefinition
{
    /// <summary>
    /// Minimum required version of assets.
    /// </summary>
    public required string MinimumVersion { get; init; }

    /// <summary>
    /// List of files that must exist for compliance.
    /// </summary>
    public IReadOnlyList<string> RequiredFiles { get; init; } = [];

    /// <summary>
    /// Regex patterns that are not allowed in asset content (e.g., secrets).
    /// </summary>
    public IReadOnlyList<string> RestrictedPatterns { get; init; } = [];

    /// <summary>
    /// Whether to enforce validation in CI pipelines.
    /// </summary>
    public bool EnforceInCi { get; init; } = true;
}
