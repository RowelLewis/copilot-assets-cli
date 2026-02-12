using DevTools.CopilotAssets.Domain;
using DevTools.CopilotAssets.Services.Adapters;
using FluentAssertions;

namespace DevTools.CopilotAssets.Tests.Services.Adapters;

public class WindsurfOutputAdapterTests
{
    private readonly WindsurfOutputAdapter _sut = new();

    [Fact]
    public void Target_ShouldBeWindsurf()
    {
        _sut.Target.Should().Be(TargetTool.Windsurf);
    }

    [Fact]
    public void GetOutputPath_Instruction_ShouldReturnWindsurfrules()
    {
        var result = _sut.GetOutputPath(AssetType.Instruction, "copilot-instructions.md");

        result.Should().Be(".windsurfrules");
    }

    [Fact]
    public void GetOutputPath_Prompt_ShouldReturnWindsurfRulesDirectory()
    {
        var result = _sut.GetOutputPath(AssetType.Prompt, "code-review.prompt.md");

        result.Should().Be(Path.Combine(".windsurf", "rules", "code-review.prompt.md"));
    }

    [Fact]
    public void GetManagedDirectories_ShouldReturnWindsurf()
    {
        _sut.GetManagedDirectories().Should().ContainSingle().Which.Should().Be(".windsurf");
    }
}

public class ClineOutputAdapterTests
{
    private readonly ClineOutputAdapter _sut = new();

    [Fact]
    public void Target_ShouldBeCline()
    {
        _sut.Target.Should().Be(TargetTool.Cline);
    }

    [Fact]
    public void GetOutputPath_Instruction_ShouldReturnClinerules()
    {
        var result = _sut.GetOutputPath(AssetType.Instruction, "copilot-instructions.md");

        result.Should().Be(Path.Combine(".clinerules", "instructions.md"));
    }

    [Fact]
    public void GetOutputPath_Prompt_ShouldReturnClinerulesDirectory()
    {
        var result = _sut.GetOutputPath(AssetType.Prompt, "code-review.prompt.md");

        result.Should().Be(Path.Combine(".clinerules", "code-review.prompt.md"));
    }

    [Fact]
    public void GetManagedDirectories_ShouldReturnClinerules()
    {
        _sut.GetManagedDirectories().Should().ContainSingle().Which.Should().Be(".clinerules");
    }
}

public class AiderOutputAdapterTests
{
    private readonly AiderOutputAdapter _sut = new();

    [Fact]
    public void Target_ShouldBeAider()
    {
        _sut.Target.Should().Be(TargetTool.Aider);
    }

    [Fact]
    public void GetOutputPath_Instruction_ShouldReturnConventionsMd()
    {
        var result = _sut.GetOutputPath(AssetType.Instruction, "copilot-instructions.md");

        result.Should().Be("CONVENTIONS.md");
    }

    [Fact]
    public void GetOutputPath_Prompt_ShouldReturnAiderPromptsDirectory()
    {
        var result = _sut.GetOutputPath(AssetType.Prompt, "code-review.prompt.md");

        result.Should().Be(Path.Combine(".aider", "prompts", "code-review.prompt.md"));
    }

    [Fact]
    public void GetManagedDirectories_ShouldReturnAider()
    {
        _sut.GetManagedDirectories().Should().ContainSingle().Which.Should().Be(".aider");
    }
}
