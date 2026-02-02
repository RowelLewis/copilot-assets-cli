namespace DevTools.CopilotAssets.Domain;

/// <summary>
/// Filter for selecting specific asset types during operations.
/// </summary>
public sealed class AssetTypeFilter
{
    private static readonly Dictionary<string, AssetCategory> TypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["instruction"] = AssetCategory.Instruction,
        ["instructions"] = AssetCategory.Instruction,
        ["prompt"] = AssetCategory.Prompts,
        ["prompts"] = AssetCategory.Prompts,
        ["agent"] = AssetCategory.Agents,
        ["agents"] = AssetCategory.Agents,
        ["skill"] = AssetCategory.Skills,
        ["skills"] = AssetCategory.Skills
    };

    public HashSet<AssetCategory> IncludedTypes { get; }

    private AssetTypeFilter(HashSet<AssetCategory> includedTypes)
    {
        IncludedTypes = includedTypes;
    }

    /// <summary>
    /// Parse an --only filter value.
    /// </summary>
    public static (bool Success, AssetTypeFilter? Filter, string? Error) ParseOnly(string value)
    {
        var types = ParseTypes(value);
        if (!types.Success)
            return (false, null, types.Error);

        return (true, new AssetTypeFilter(types.Types!), null);
    }

    /// <summary>
    /// Parse an --exclude filter value.
    /// </summary>
    public static (bool Success, AssetTypeFilter? Filter, string? Error) ParseExclude(string value)
    {
        var types = ParseTypes(value);
        if (!types.Success)
            return (false, null, types.Error);

        // Convert exclude to include by taking complement
        var allTypes = Enum.GetValues<AssetCategory>().ToHashSet();
        allTypes.ExceptWith(types.Types!);

        if (allTypes.Count == 0)
            return (false, null, "Cannot exclude all asset types");

        return (true, new AssetTypeFilter(allTypes), null);
    }

    private static (bool Success, HashSet<AssetCategory>? Types, string? Error) ParseTypes(string value)
    {
        var result = new HashSet<AssetCategory>();
        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            if (!TypeMap.TryGetValue(part, out var category))
            {
                return (false, null, $"Unknown asset type: '{part}'. Valid types: instruction, prompts, agents, skills");
            }
            result.Add(category);
        }

        if (result.Count == 0)
            return (false, null, "No valid asset types specified");

        return (true, result, null);
    }

    /// <summary>
    /// Check if the specified category should be included.
    /// </summary>
    public bool ShouldInclude(AssetCategory category) => IncludedTypes.Contains(category);

    /// <summary>
    /// Determine the category of an asset based on its relative path.
    /// </summary>
    public static AssetCategory GetCategory(string relativePath)
    {
        if (relativePath.Contains("prompts/") || relativePath.Contains("prompts\\"))
            return AssetCategory.Prompts;
        if (relativePath.Contains("agents/") || relativePath.Contains("agents\\"))
            return AssetCategory.Agents;
        if (relativePath.Contains("skills/") || relativePath.Contains("skills\\"))
            return AssetCategory.Skills;
        if (relativePath.EndsWith("copilot-instructions.md", StringComparison.OrdinalIgnoreCase))
            return AssetCategory.Instruction;

        // Default to prompts for unknown files
        return AssetCategory.Prompts;
    }

    /// <summary>
    /// Check if a path should be included based on its category.
    /// </summary>
    public bool ShouldIncludePath(string relativePath) =>
        ShouldInclude(GetCategory(relativePath));

    /// <summary>
    /// Get the filter mode description for display.
    /// </summary>
    public string GetDescription()
    {
        var types = IncludedTypes.Select(t => t.ToString().ToLowerInvariant());
        return string.Join(", ", types);
    }
}
