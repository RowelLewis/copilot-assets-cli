namespace DevTools.CopilotAssets.Domain;

/// <summary>
/// Maps an asset type + target tool to a concrete file path and format.
/// </summary>
public sealed record OutputMapping
{
    /// <summary>
    /// The target tool for this mapping.
    /// </summary>
    public required TargetTool Target { get; init; }

    /// <summary>
    /// The asset type being mapped.
    /// </summary>
    public required AssetType Type { get; init; }

    /// <summary>
    /// Output file path relative to the project root.
    /// </summary>
    public required string OutputPath { get; init; }

    /// <summary>
    /// The output format (e.g., markdown, mdc, plaintext, yaml).
    /// </summary>
    public required string Format { get; init; }
}
