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

    public string GetOutputPath(AssetType type, string relativePath)
    {
        var fileName = Path.GetFileName(relativePath);

        // Handle instructions from folder - add to root as separate files
        if (type == AssetType.Instruction && (relativePath.Contains("instructions/") || relativePath.Contains("instructions\\")))
        {
            // Aider doesn't have an instructions folder, so prepend filename (e.g., coding-standards.md)
            return Path.ChangeExtension(fileName, ".md");
        }
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
