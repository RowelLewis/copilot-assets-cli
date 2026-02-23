namespace DevTools.CopilotAssets.Domain;

/// <summary>
/// Represents a parsed SKILL.md file following the cross-tool standard.
/// </summary>
public sealed record SkillDefinition
{
    /// <summary>
    /// Skill name from frontmatter.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Skill description from frontmatter.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Markdown body content after frontmatter.
    /// </summary>
    public required string Body { get; init; }

    /// <summary>
    /// Additional metadata from frontmatter.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }
}
