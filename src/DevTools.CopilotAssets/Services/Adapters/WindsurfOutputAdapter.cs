using DevTools.CopilotAssets.Domain;

namespace DevTools.CopilotAssets.Services.Adapters;

/// <summary>
/// Output adapter for Windsurf. Generates .windsurfrules file.
/// </summary>
public sealed class WindsurfOutputAdapter : IOutputAdapter
{
    public TargetTool Target => TargetTool.Windsurf;

    public string TransformContent(string markdownContent, AssetMetadata metadata)
    {
        return CopilotOutputAdapter.StripToolSpecificSections(markdownContent, "windsurf");
    }

    public string GetOutputPath(AssetType type, string fileName)
    {
        // Windsurf uses a single .windsurfrules file for instructions
        // and .windsurf/rules/ for other assets
        return type switch
        {
            AssetType.Instruction => ".windsurfrules",
            AssetType.Prompt => Path.Combine(".windsurf", "rules", Path.ChangeExtension(fileName, ".md")),
            AssetType.Agent => Path.Combine(".windsurf", "rules", Path.ChangeExtension(fileName, ".md")),
            AssetType.Skill => Path.Combine(".windsurf", "rules", Path.ChangeExtension(fileName, ".md")),
            _ => Path.Combine(".windsurf", "rules", fileName)
        };
    }

    public IEnumerable<string> GetManagedDirectories()
    {
        yield return ".windsurf";
    }
}
