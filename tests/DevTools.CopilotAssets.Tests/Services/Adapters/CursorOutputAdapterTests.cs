using DevTools.CopilotAssets.Domain;
using DevTools.CopilotAssets.Services.Adapters;
using FluentAssertions;

namespace DevTools.CopilotAssets.Tests.Services.Adapters;

public class CursorOutputAdapterTests
{
    private readonly CursorOutputAdapter _sut = new();

    [Fact]
    public void Target_ShouldBeCursor()
    {
        _sut.Target.Should().Be(TargetTool.Cursor);
    }

    [Fact]
    public void GetOutputPath_Instruction_ShouldReturnInstructionsMdc()
    {
        var result = _sut.GetOutputPath(AssetType.Instruction, "copilot-instructions.md");

        result.Should().Be(Path.Combine(".cursor", "rules", "instructions.mdc"));
    }

    [Fact]
    public void GetOutputPath_Prompt_ShouldReturnMdcFile()
    {
        var result = _sut.GetOutputPath(AssetType.Prompt, "code-review.prompt.md");

        result.Should().Be(Path.Combine(".cursor", "rules", "code-review.mdc"));
    }

    [Fact]
    public void GetManagedDirectories_ShouldReturnCursorRules()
    {
        var dirs = _sut.GetManagedDirectories().ToList();

        dirs.Should().ContainSingle().Which.Should().Be(Path.Combine(".cursor", "rules"));
    }

    [Fact]
    public void TransformContent_ShouldAddYamlFrontmatter()
    {
        var content = "# Code Review Prompt\n\nReview the following code.";
        var metadata = new AssetMetadata
        {
            FileName = "code-review.prompt.md",
            RelativePath = "prompts/code-review.prompt.md",
            Type = AssetType.Prompt
        };

        var result = _sut.TransformContent(content, metadata);

        result.Should().StartWith("---");
        result.Should().Contain("description: Code Review Prompt");
        result.Should().Contain("alwaysApply: true");
        result.Should().Contain("---\n");
        result.Should().Contain("Review the following code.");
    }

    [Fact]
    public void TransformContent_WithNoHeading_ShouldUseFileNameAsDescription()
    {
        var content = "No heading here, just content.";
        var metadata = new AssetMetadata
        {
            FileName = "test.md",
            RelativePath = "prompts/test.md",
            Type = AssetType.Prompt
        };

        var result = _sut.TransformContent(content, metadata);

        result.Should().Contain("description: test.md");
    }

    [Fact]
    public void TransformContent_ShouldKeepCursorSectionsOnly()
    {
        var content = """
            # Rules

            General.

            <!-- cursor-only -->
            Cursor globs config.
            <!-- /cursor-only -->

            <!-- copilot-only -->
            Copilot stuff.
            <!-- /copilot-only -->
            """;

        var metadata = new AssetMetadata
        {
            FileName = "rules.md",
            RelativePath = "copilot-instructions.md",
            Type = AssetType.Instruction
        };

        var result = _sut.TransformContent(content, metadata);

        result.Should().Contain("Cursor globs config.");
        result.Should().NotContain("Copilot stuff.");
    }
}
