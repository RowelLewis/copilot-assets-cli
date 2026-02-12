using DevTools.CopilotAssets.Domain;

namespace DevTools.CopilotAssets.Services.Adapters;

/// <summary>
/// Output adapter for Aider. Generates .aider.conf.yml conventions section.
/// </summary>
public sealed class AiderOutputAdapter : IOutputAdapter
{
    public TargetTool Target => TargetTool.Aider;

    public string TransformContent(string markdownContent, AssetMetadata metadata)
    {
        var content = CopilotOutputAdapter.StripToolSpecificSections(markdownContent, "aider");

        // For instructions, format as CONVENTIONS.md
        if (metadata.Type == AssetType.Instruction)
        {
            return content;
        }

        return content;
    }

    public string GetOutputPath(AssetType type, string fileName)
    {
        return type switch
        {
            AssetType.Instruction => "CONVENTIONS.md",
            AssetType.Prompt => Path.Combine(".aider", "prompts", Path.ChangeExtension(fileName, ".md")),
            AssetType.Agent => Path.Combine(".aider", "prompts", Path.ChangeExtension(fileName, ".md")),
            AssetType.Skill => Path.Combine(".aider", "prompts", Path.ChangeExtension(fileName, ".md")),
            _ => Path.Combine(".aider", fileName)
        };
    }

    public IEnumerable<string> GetManagedDirectories()
    {
        yield return ".aider";
    }
}
