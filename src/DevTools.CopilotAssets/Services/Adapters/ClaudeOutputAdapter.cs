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

    public string GetOutputPath(AssetType type, string relativePath)
    {
        var fileName = Path.GetFileName(relativePath);
        return type switch
        {
            AssetType.Instruction => GetClaudeInstructionPath(relativePath),
            AssetType.Prompt => Path.Combine(".claude", "commands", Path.ChangeExtension(fileName, ".md")),
            AssetType.Agent => Path.Combine(".claude", "commands", Path.ChangeExtension(fileName, ".md")),
            AssetType.Skill => ExtractClaudeSkillPath(relativePath),
            _ => Path.Combine(".claude", fileName)
        };
    }

    private static string GetClaudeInstructionPath(string relativePath)
    {
        // For files in instructions/ folder, put in .claude/instructions/
        if (relativePath.Contains("instructions/") || relativePath.Contains("instructions\\"))
        {
            return Path.Combine(".claude", "instructions", Path.GetFileName(relativePath));
        }
        // For root-level instruction files, use CLAUDE.md
        return "CLAUDE.md";
    }

    private static string ExtractClaudeSkillPath(string relativePath)
    {
        // For paths like "skills/refactor/SKILL.md", extract "refactor"
        var parts = relativePath.Replace("\\", "/").Split('/');
        if (parts.Length >= 3 && parts[0] == "skills")
        {
            return Path.Combine(".claude", "skills", parts[1], "SKILL.md");
        }
        // Fallback
        return Path.Combine(".claude", "skills", Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(Path.GetFileName(relativePath))), "SKILL.md");
    }

    public IEnumerable<string> GetManagedDirectories()
    {
        yield return ".claude";
    }
}
