namespace DevTools.CopilotAssets.Domain;

/// <summary>
/// Defines policy rules for Copilot asset compliance validation.
/// </summary>
public sealed record PolicyDefinition
{
    /// <summary>
    /// Files that must exist for compliance.
    /// </summary>
    public required IReadOnlyList<string> RequiredFiles { get; init; }

    /// <summary>
    /// Regex patterns that should not appear in assets (e.g., secrets).
    /// </summary>
    public IReadOnlyList<string> RestrictedPatterns { get; init; } = [];

    /// <summary>
    /// Whether to enforce validation in CI pipelines.
    /// </summary>
    public bool EnforceInCi { get; init; } = true;
}
