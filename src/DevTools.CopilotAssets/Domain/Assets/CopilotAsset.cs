namespace DevTools.CopilotAssets.Domain;

/// <summary>
/// Represents a single Copilot asset (prompt, agent, instruction, or skill).
/// </summary>
public sealed record CopilotAsset
{
    /// <summary>
    /// Asset name (e.g., "code-review.prompt.md").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Source path within the templates directory.
    /// </summary>
    public required string SourcePath { get; init; }

    /// <summary>
    /// Target path relative to the project root.
    /// </summary>
    public required string TargetPath { get; init; }

    /// <summary>
    /// Asset type category.
    /// </summary>
    public required AssetType Type { get; init; }

    /// <summary>
    /// SHA256 checksum for integrity verification.
    /// </summary>
    public string? Checksum { get; init; }
}


