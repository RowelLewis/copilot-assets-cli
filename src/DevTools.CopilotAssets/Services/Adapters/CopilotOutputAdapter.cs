using DevTools.CopilotAssets.Domain;

namespace DevTools.CopilotAssets.Services.Adapters;

/// <summary>
/// Output adapter for GitHub Copilot. Pass-through to .github/ directory.
/// </summary>
public sealed class CopilotOutputAdapter : IOutputAdapter
{
    public TargetTool Target => TargetTool.Copilot;

    public string TransformContent(string markdownContent, AssetMetadata metadata)
    {
        return StripToolSpecificSections(markdownContent, "copilot");
    }

    public string GetOutputPath(AssetType type, string fileName)
    {
        return type switch
        {
            AssetType.Instruction => Path.Combine(".github", "copilot-instructions.md"),
            AssetType.Prompt => Path.Combine(".github", "prompts", fileName),
            AssetType.Agent => Path.Combine(".github", "agents", fileName),
            AssetType.Skill => Path.Combine(".github", "skills", Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(fileName)), "SKILL.md"),
            _ => Path.Combine(".github", fileName)
        };
    }

    public IEnumerable<string> GetManagedDirectories()
    {
        yield return ".github";
    }

    internal static string StripToolSpecificSections(string content, string keepTool)
    {
        var result = content;
        var tools = new[] { "copilot", "claude", "cursor", "windsurf", "cline", "aider" };

        foreach (var tool in tools)
        {
            if (tool == keepTool) continue;

            // Remove sections for other tools: <!-- tool-only --> ... <!-- /tool-only -->
            var startTag = $"<!-- {tool}-only -->";
            var endTag = $"<!-- /{tool}-only -->";

            while (true)
            {
                var startIdx = result.IndexOf(startTag, StringComparison.OrdinalIgnoreCase);
                if (startIdx < 0) break;

                var endIdx = result.IndexOf(endTag, startIdx, StringComparison.OrdinalIgnoreCase);
                if (endIdx < 0) break;

                result = result[..startIdx] + result[(endIdx + endTag.Length)..];
            }
        }

        // Remove the keep tool's markers but keep the content
        var keepStart = $"<!-- {keepTool}-only -->";
        var keepEnd = $"<!-- /{keepTool}-only -->";
        result = result.Replace(keepStart, "", StringComparison.OrdinalIgnoreCase);
        result = result.Replace(keepEnd, "", StringComparison.OrdinalIgnoreCase);

        // Clean up excessive blank lines
        while (result.Contains("\n\n\n"))
        {
            result = result.Replace("\n\n\n", "\n\n");
        }

        return result.Trim() + "\n";
    }
}
