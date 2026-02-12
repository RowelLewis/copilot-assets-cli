using DevTools.CopilotAssets.Domain;

namespace DevTools.CopilotAssets.Services.Adapters;

/// <summary>
/// Metadata about an asset being transformed.
/// </summary>
public sealed record AssetMetadata
{
    /// <summary>
    /// Original file name from the template.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Original relative path from the template.
    /// </summary>
    public required string RelativePath { get; init; }

    /// <summary>
    /// Asset type category.
    /// </summary>
    public required AssetType Type { get; init; }
}

/// <summary>
/// Transforms asset content and determines output paths for a specific target tool.
/// </summary>
public interface IOutputAdapter
{
    /// <summary>
    /// The target tool this adapter generates output for.
    /// </summary>
    TargetTool Target { get; }

    /// <summary>
    /// Transform content for the target tool.
    /// </summary>
    string TransformContent(string markdownContent, AssetMetadata metadata);

    /// <summary>
    /// Get the output file path relative to the project root.
    /// </summary>
    string GetOutputPath(AssetType type, string relativePath);

    /// <summary>
    /// Get all directories managed by this adapter (relative to project root).
    /// </summary>
    IEnumerable<string> GetManagedDirectories();
}
