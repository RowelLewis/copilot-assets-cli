using DevTools.CopilotAssets.Domain;

namespace DevTools.CopilotAssets.Services.Adapters;

/// <summary>
/// Output adapter for Cursor. Generates .cursor/rules/*.mdc files with YAML frontmatter.
/// </summary>
public sealed class CursorOutputAdapter : IOutputAdapter
{
    public TargetTool Target => TargetTool.Cursor;

    public string TransformContent(string markdownContent, AssetMetadata metadata)
    {
        var content = CopilotOutputAdapter.StripToolSpecificSections(markdownContent, "cursor");

        // Wrap in .mdc format with YAML frontmatter
        var description = ExtractFirstHeading(content) ?? metadata.FileName;
        var frontmatter = $"""
            ---
            description: {description}
            alwaysApply: true
            ---
            """;

        return frontmatter + "\n" + content;
    }

    public string GetOutputPath(AssetType type, string fileName)
    {
        var mdcName = Path.ChangeExtension(Path.GetFileNameWithoutExtension(fileName), ".mdc");

        return type switch
        {
            AssetType.Instruction => Path.Combine(".cursor", "rules", "instructions.mdc"),
            AssetType.Prompt => Path.Combine(".cursor", "rules", mdcName),
            AssetType.Agent => Path.Combine(".cursor", "rules", mdcName),
            AssetType.Skill => Path.Combine(".cursor", "rules", mdcName),
            _ => Path.Combine(".cursor", "rules", mdcName)
        };
    }

    public IEnumerable<string> GetManagedDirectories()
    {
        yield return Path.Combine(".cursor", "rules");
    }

    private static string? ExtractFirstHeading(string content)
    {
        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith('#'))
            {
                return trimmed.TrimStart('#').Trim();
            }
        }
        return null;
    }
}
