using DevTools.CopilotAssets.Domain;

namespace DevTools.CopilotAssets.Services.Skills;

/// <summary>
/// Parses SKILL.md files with YAML frontmatter and Markdown body.
/// </summary>
public static class SkillParser
{
    private const string FrontmatterDelimiter = "---";

    /// <summary>
    /// Parse a SKILL.md file content into a SkillDefinition.
    /// </summary>
    /// <param name="content">Raw file content.</param>
    /// <returns>Parsed skill definition, or null if not a valid SKILL.md.</returns>
    public static SkillDefinition? Parse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        var (frontmatter, body) = SplitFrontmatter(content);

        if (frontmatter == null)
        {
            // No frontmatter — try to extract name from first heading
            var heading = ExtractFirstHeading(content);
            if (heading == null) return null;

            return new SkillDefinition
            {
                Name = heading,
                Description = ExtractFirstParagraph(content) ?? heading,
                Body = content
            };
        }

        var fields = ParseYamlFields(frontmatter);

        if (!fields.TryGetValue("name", out var name) || string.IsNullOrWhiteSpace(name))
            return null;

        fields.TryGetValue("description", out var description);

        // Collect extra metadata
        var metadata = new Dictionary<string, string>();
        foreach (var (key, value) in fields)
        {
            if (key is not ("name" or "description") && !string.IsNullOrWhiteSpace(value))
            {
                metadata[key] = value;
            }
        }

        return new SkillDefinition
        {
            Name = name,
            Description = description ?? name,
            Body = body ?? string.Empty,
            Metadata = metadata.Count > 0 ? metadata : null
        };
    }

    /// <summary>
    /// Validate that a SKILL.md has the required fields.
    /// </summary>
    /// <returns>List of validation errors (empty if valid).</returns>
    public static IReadOnlyList<string> Validate(string content, string filePath)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(content))
        {
            errors.Add($"SKILL.md file is empty: {filePath}");
            return errors;
        }

        var skill = Parse(content);
        if (skill == null)
        {
            errors.Add($"SKILL.md missing required 'name' field or heading: {filePath}");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(skill.Description) || skill.Description == skill.Name)
        {
            // Only warn, not an error — description defaulting to name is acceptable
        }

        if (string.IsNullOrWhiteSpace(skill.Body))
        {
            errors.Add($"SKILL.md has empty body: {filePath}");
        }

        return errors;
    }

    /// <summary>
    /// Generate SKILL.md content from a SkillDefinition.
    /// </summary>
    public static string Generate(SkillDefinition skill)
    {
        var frontmatter = $"""
            ---
            name: {skill.Name}
            description: {skill.Description}
            ---
            """;

        return frontmatter + "\n" + skill.Body;
    }

    private static (string? Frontmatter, string? Body) SplitFrontmatter(string content)
    {
        var trimmed = content.TrimStart();
        if (!trimmed.StartsWith(FrontmatterDelimiter))
            return (null, content);

        var afterFirst = trimmed.IndexOf('\n');
        if (afterFirst < 0) return (null, content);

        var remaining = trimmed[(afterFirst + 1)..];
        var endIdx = remaining.IndexOf($"\n{FrontmatterDelimiter}", StringComparison.Ordinal);
        if (endIdx < 0) return (null, content);

        var frontmatter = remaining[..endIdx].Trim();
        var body = remaining[(endIdx + FrontmatterDelimiter.Length + 1)..].TrimStart('\n', '\r', '-');

        // Skip past the closing delimiter line
        var bodyStart = body.IndexOf('\n');
        if (bodyStart >= 0)
            body = body[(bodyStart + 1)..];

        return (frontmatter, body.Trim());
    }

    private static Dictionary<string, string> ParseYamlFields(string yaml)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in yaml.Split('\n'))
        {
            var colonIdx = line.IndexOf(':');
            if (colonIdx <= 0) continue;

            var key = line[..colonIdx].Trim().ToLowerInvariant();
            var value = line[(colonIdx + 1)..].Trim();

            // Remove surrounding quotes if present
            if (value.Length >= 2 &&
                ((value.StartsWith('"') && value.EndsWith('"')) ||
                 (value.StartsWith('\'') && value.EndsWith('\''))))
            {
                value = value[1..^1];
            }

            fields[key] = value;
        }

        return fields;
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

    private static string? ExtractFirstParagraph(string content)
    {
        var pastHeading = false;
        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith('#'))
            {
                pastHeading = true;
                continue;
            }
            if (pastHeading && !string.IsNullOrWhiteSpace(trimmed))
            {
                return trimmed;
            }
        }
        return null;
    }
}
