using DevTools.CopilotAssets.Domain;
using DevTools.CopilotAssets.Services.Adapters;
using FluentAssertions;

namespace DevTools.CopilotAssets.Tests.Services.Adapters;

public class ClaudeOutputAdapterTests
{
    private readonly ClaudeOutputAdapter _sut = new();

    [Fact]
    public void Target_ShouldBeClaude()
    {
        _sut.Target.Should().Be(TargetTool.Claude);
    }

    [Fact]
    public void GetOutputPath_Instruction_ShouldReturnClaudeMd()
    {
        var result = _sut.GetOutputPath(AssetType.Instruction, "copilot-instructions.md");

        result.Should().Be("CLAUDE.md");
    }

    [Fact]
    public void GetOutputPath_Prompt_ShouldReturnClaudeCommands()
    {
        var result = _sut.GetOutputPath(AssetType.Prompt, "code-review.prompt.md");

        result.Should().Be(Path.Combine(".claude", "commands", "code-review.prompt.md"));
    }

    [Fact]
    public void GetOutputPath_Agent_ShouldReturnClaudeCommands()
    {
        var result = _sut.GetOutputPath(AssetType.Agent, "reviewer.agent.md");

        result.Should().Be(Path.Combine(".claude", "commands", "reviewer.agent.md"));
    }

    [Fact]
    public void GetOutputPath_Skill_ShouldReturnClaudeSkillMd()
    {
        var result = _sut.GetOutputPath(AssetType.Skill, "refactor.skill.md");

        result.Should().Be(Path.Combine(".claude", "skills", "refactor", "SKILL.md"));
    }

    [Fact]
    public void GetManagedDirectories_ShouldReturnClaudeDirectory()
    {
        var dirs = _sut.GetManagedDirectories().ToList();

        dirs.Should().ContainSingle().Which.Should().Be(".claude");
    }

    [Fact]
    public void TransformContent_ShouldKeepClaudeSections()
    {
        var content = """
            # Instructions

            General.

            <!-- claude-only -->
            Claude-specific.
            <!-- /claude-only -->

            <!-- copilot-only -->
            Copilot-specific.
            <!-- /copilot-only -->
            """;

        var metadata = new AssetMetadata
        {
            FileName = "instructions.md",
            RelativePath = "copilot-instructions.md",
            Type = AssetType.Instruction
        };

        var result = _sut.TransformContent(content, metadata);

        result.Should().Contain("General.");
        result.Should().Contain("Claude-specific.");
        result.Should().NotContain("Copilot-specific.");
    }
}
