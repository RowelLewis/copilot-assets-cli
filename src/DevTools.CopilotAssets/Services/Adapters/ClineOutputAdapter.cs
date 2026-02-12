using DevTools.CopilotAssets.Domain;

namespace DevTools.CopilotAssets.Services.Adapters;

/// <summary>
/// Output adapter for Cline. Generates .clinerules/*.md files.
/// </summary>
public sealed class ClineOutputAdapter : IOutputAdapter
{
    public TargetTool Target => TargetTool.Cline;

    public string TransformContent(string markdownContent, AssetMetadata metadata)
    {
        return CopilotOutputAdapter.StripToolSpecificSections(markdownContent, "cline");
    }

    public string GetOutputPath(AssetType type, string relativePath)
    {
        var fileName = Path.GetFileName(relativePath);
        var mdName = Path.ChangeExtension(fileName, ".md");

        // Handle instructions from folder
        if (type == AssetType.Instruction && (relativePath.Contains("instructions/") || relativePath.Contains("instructions\\")))
        {
            return Path.Combine(".clinerules", mdName);
        }

        return type switch
        {
            AssetType.Instruction => Path.Combine(".clinerules", "instructions.md"),
            AssetType.Prompt => Path.Combine(".clinerules", mdName),
            AssetType.Agent => Path.Combine(".clinerules", mdName),
            AssetType.Skill => Path.Combine(".clinerules", mdName),
            _ => Path.Combine(".clinerules", mdName)
        };
    }

    public IEnumerable<string> GetManagedDirectories()
    {
        yield return ".clinerules";
    }
}
