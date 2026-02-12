using DevTools.CopilotAssets.Domain;

namespace DevTools.CopilotAssets.Services.Adapters;

/// <summary>
/// Output adapter for Claude Code. Generates CLAUDE.md and .claude/ directory structure.
/// </summary>
public sealed class ClaudeOutputAdapter : IOutputAdapter
{
    public TargetTool Target => TargetTool.Claude;

    public string TransformContent(string markdownContent, AssetMetadata metadata)
    {
        return CopilotOutputAdapter.StripToolSpecificSections(markdownContent, "claude");
    }

    public string GetOutputPath(AssetType type, string fileName)
    {
        return type switch
        {
            AssetType.Instruction => "CLAUDE.md",
            AssetType.Prompt => Path.Combine(".claude", "commands", Path.ChangeExtension(fileName, ".md")),
            AssetType.Agent => Path.Combine(".claude", "commands", Path.ChangeExtension(fileName, ".md")),
            AssetType.Skill => Path.Combine(".claude", "skills", Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(fileName)), "SKILL.md"),
            _ => Path.Combine(".claude", fileName)
        };
    }

    public IEnumerable<string> GetManagedDirectories()
    {
        yield return ".claude";
    }
}
