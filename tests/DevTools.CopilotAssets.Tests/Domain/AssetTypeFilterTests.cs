namespace DevTools.CopilotAssets.Tests.Domain;

using DevTools.CopilotAssets.Domain;

public class AssetTypeFilterTests
{
    [Theory]
    [InlineData("prompts", true)]
    [InlineData("PROMPTS", true)]
    [InlineData("prompt", true)]
    [InlineData("agents", true)]
    [InlineData("skills", true)]
    [InlineData("instruction", true)]
    [InlineData("instructions", true)]
    [InlineData("invalid", false)]
    [InlineData("", false)]
    public void ParseOnly_ValidatesInput(string input, bool shouldSucceed)
    {
        var result = AssetTypeFilter.ParseOnly(input);
        Assert.Equal(shouldSucceed, result.Success);
        if (shouldSucceed)
        {
            Assert.NotNull(result.Filter);
        }
        else
        {
            Assert.Null(result.Filter);
            Assert.NotNull(result.Error);
        }
    }

    [Fact]
    public void ParseOnly_MultipleTypes_ParsesCorrectly()
    {
        var result = AssetTypeFilter.ParseOnly("prompts,agents");

        Assert.True(result.Success);
        Assert.NotNull(result.Filter);
        Assert.True(result.Filter!.ShouldInclude(AssetCategory.Prompts));
        Assert.True(result.Filter!.ShouldInclude(AssetCategory.Agents));
        Assert.False(result.Filter!.ShouldInclude(AssetCategory.Skills));
        Assert.False(result.Filter!.ShouldInclude(AssetCategory.Instruction));
    }

    [Fact]
    public void ParseOnly_WithSpaces_ParsesCorrectly()
    {
        var result = AssetTypeFilter.ParseOnly("prompts, agents, skills");

        Assert.True(result.Success);
        Assert.NotNull(result.Filter);
        Assert.True(result.Filter!.ShouldInclude(AssetCategory.Prompts));
        Assert.True(result.Filter!.ShouldInclude(AssetCategory.Agents));
        Assert.True(result.Filter!.ShouldInclude(AssetCategory.Skills));
    }

    [Fact]
    public void ParseExclude_InvertsSelection()
    {
        var result = AssetTypeFilter.ParseExclude("skills");

        Assert.True(result.Success);
        Assert.NotNull(result.Filter);
        Assert.True(result.Filter!.ShouldInclude(AssetCategory.Prompts));
        Assert.True(result.Filter!.ShouldInclude(AssetCategory.Agents));
        Assert.True(result.Filter!.ShouldInclude(AssetCategory.Instruction));
        Assert.False(result.Filter!.ShouldInclude(AssetCategory.Skills));
    }

    [Fact]
    public void ParseExclude_MultipleTypes_InvertsAll()
    {
        var result = AssetTypeFilter.ParseExclude("skills,agents");

        Assert.True(result.Success);
        Assert.NotNull(result.Filter);
        Assert.True(result.Filter!.ShouldInclude(AssetCategory.Prompts));
        Assert.True(result.Filter!.ShouldInclude(AssetCategory.Instruction));
        Assert.False(result.Filter!.ShouldInclude(AssetCategory.Agents));
        Assert.False(result.Filter!.ShouldInclude(AssetCategory.Skills));
    }

    [Fact]
    public void ParseExclude_AllTypes_ReturnsError()
    {
        var result = AssetTypeFilter.ParseExclude("instruction,prompts,agents,skills");

        Assert.False(result.Success);
        Assert.Null(result.Filter);
        Assert.Contains("Cannot exclude all", result.Error);
    }

    [Theory]
    [InlineData("prompts/code-review.md", AssetCategory.Prompts)]
    [InlineData("prompts\\bug-fix.md", AssetCategory.Prompts)]
    [InlineData("agents/gilfoyle.md", AssetCategory.Agents)]
    [InlineData("agents\\plan.md", AssetCategory.Agents)]
    [InlineData("skills/testing.md", AssetCategory.Skills)]
    [InlineData("skills\\analysis.md", AssetCategory.Skills)]
    [InlineData("copilot-instructions.md", AssetCategory.Instruction)]
    public void GetCategory_ReturnsCorrectCategory(string path, AssetCategory expected)
    {
        var result = AssetTypeFilter.GetCategory(path);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ShouldIncludePath_WithPromptsFilter_IncludesOnlyPrompts()
    {
        var filter = AssetTypeFilter.ParseOnly("prompts").Filter!;

        Assert.True(filter.ShouldIncludePath("prompts/code-review.md"));
        Assert.False(filter.ShouldIncludePath("agents/gilfoyle.md"));
        Assert.False(filter.ShouldIncludePath("skills/testing.md"));
        Assert.False(filter.ShouldIncludePath("copilot-instructions.md"));
    }

    [Fact]
    public void ShouldIncludePath_WithExcludeSkillsFilter_ExcludesOnlySkills()
    {
        var filter = AssetTypeFilter.ParseExclude("skills").Filter!;

        Assert.True(filter.ShouldIncludePath("prompts/code-review.md"));
        Assert.True(filter.ShouldIncludePath("agents/gilfoyle.md"));
        Assert.True(filter.ShouldIncludePath("copilot-instructions.md"));
        Assert.False(filter.ShouldIncludePath("skills/testing.md"));
    }

    [Fact]
    public void GetDescription_ReturnsCategoriesLowercase()
    {
        var filter = AssetTypeFilter.ParseOnly("prompts,agents").Filter!;
        var description = filter.GetDescription();

        Assert.Contains("prompts", description);
        Assert.Contains("agents", description);
    }

    [Theory]
    [InlineData("foo")]
    [InlineData("bar,baz")]
    [InlineData("prompts,invalid")]
    public void ParseOnly_InvalidType_ReturnsError(string input)
    {
        var result = AssetTypeFilter.ParseOnly(input);

        Assert.False(result.Success);
        Assert.Null(result.Filter);
        Assert.Contains("Unknown asset type", result.Error);
    }
}
