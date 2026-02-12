using DevTools.CopilotAssets.Domain;
using DevTools.CopilotAssets.Services.Adapters;
using FluentAssertions;

namespace DevTools.CopilotAssets.Tests.Services.Adapters;

public class CopilotOutputAdapterTests
{
    private readonly CopilotOutputAdapter _sut = new();

    [Fact]
    public void Target_ShouldBeCopilot()
    {
        _sut.Target.Should().Be(TargetTool.Copilot);
    }

    [Theory]
    [InlineData(AssetType.Instruction, "copilot-instructions.md")]
    [InlineData(AssetType.Prompt, "code-review.prompt.md")]
    [InlineData(AssetType.Agent, "reviewer.agent.md")]
    [InlineData(AssetType.Skill, "refactor.skill.md")]
    public void GetOutputPath_ShouldReturnCorrectPath(AssetType type, string fileName)
    {
        var result = _sut.GetOutputPath(type, fileName);

        result.Should().StartWith(".github");
    }

    [Fact]
    public void GetOutputPath_Instruction_ShouldReturnCopilotInstructionsPath()
    {
        var result = _sut.GetOutputPath(AssetType.Instruction, "copilot-instructions.md");

        result.Should().Be(Path.Combine(".github", "copilot-instructions.md"));
    }

    [Fact]
    public void GetOutputPath_Prompt_ShouldReturnPromptsSubdirectory()
    {
        var result = _sut.GetOutputPath(AssetType.Prompt, "code-review.prompt.md");

        result.Should().Be(Path.Combine(".github", "prompts", "code-review.prompt.md"));
    }

    [Fact]
    public void GetManagedDirectories_ShouldReturnGitHub()
    {
        var dirs = _sut.GetManagedDirectories().ToList();

        dirs.Should().ContainSingle().Which.Should().Be(".github");
    }

    [Fact]
    public void TransformContent_ShouldPassThroughContent()
    {
        var content = "# Test Content\n\nSome text here.";
        var metadata = new AssetMetadata
        {
            FileName = "test.md",
            RelativePath = "prompts/test.md",
            Type = AssetType.Prompt
        };

        var result = _sut.TransformContent(content, metadata);

        result.Should().Contain("# Test Content");
        result.Should().Contain("Some text here.");
    }

    [Fact]
    public void TransformContent_ShouldStripOtherToolSections()
    {
        var content = """
            # Instructions

            General content.

            <!-- claude-only -->
            Claude-specific content.
            <!-- /claude-only -->

            <!-- copilot-only -->
            Copilot-specific content.
            <!-- /copilot-only -->

            More general content.
            """;

        var metadata = new AssetMetadata
        {
            FileName = "instructions.md",
            RelativePath = "copilot-instructions.md",
            Type = AssetType.Instruction
        };

        var result = _sut.TransformContent(content, metadata);

        result.Should().Contain("General content.");
        result.Should().Contain("Copilot-specific content.");
        result.Should().NotContain("Claude-specific content.");
        result.Should().NotContain("<!-- copilot-only -->");
        result.Should().NotContain("<!-- claude-only -->");
    }

    [Fact]
    public void TransformContent_ShouldHandleMultipleToolSections()
    {
        var content = """
            # Base

            <!-- cursor-only -->
            Cursor stuff.
            <!-- /cursor-only -->

            <!-- windsurf-only -->
            Windsurf stuff.
            <!-- /windsurf-only -->

            <!-- copilot-only -->
            Copilot stuff.
            <!-- /copilot-only -->

            End.
            """;

        var metadata = new AssetMetadata
        {
            FileName = "test.md",
            RelativePath = "copilot-instructions.md",
            Type = AssetType.Instruction
        };

        var result = _sut.TransformContent(content, metadata);

        result.Should().Contain("# Base");
        result.Should().Contain("Copilot stuff.");
        result.Should().Contain("End.");
        result.Should().NotContain("Cursor stuff.");
        result.Should().NotContain("Windsurf stuff.");
    }

    [Fact]
    public void TransformContent_WithNoSections_ShouldReturnContentUnchanged()
    {
        var content = "# Simple content\n\nNo tool-specific sections.";
        var metadata = new AssetMetadata
        {
            FileName = "test.md",
            RelativePath = "prompts/test.md",
            Type = AssetType.Prompt
        };

        var result = _sut.TransformContent(content, metadata);

        result.Should().Contain("# Simple content");
        result.Should().Contain("No tool-specific sections.");
    }
}
