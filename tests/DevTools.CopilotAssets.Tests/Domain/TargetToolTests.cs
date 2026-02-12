using DevTools.CopilotAssets.Domain;
using FluentAssertions;

namespace DevTools.CopilotAssets.Tests.Domain;

public class TargetToolTests
{
    [Theory]
    [InlineData("copilot", TargetTool.Copilot)]
    [InlineData("claude", TargetTool.Claude)]
    [InlineData("cursor", TargetTool.Cursor)]
    [InlineData("windsurf", TargetTool.Windsurf)]
    [InlineData("cline", TargetTool.Cline)]
    [InlineData("aider", TargetTool.Aider)]
    public void ParseTargets_WithSingleValidTarget_ShouldSucceed(string input, TargetTool expected)
    {
        var (success, tools, error) = TargetToolExtensions.ParseTargets(input);

        success.Should().BeTrue();
        tools.Should().ContainSingle().Which.Should().Be(expected);
        error.Should().BeNull();
    }

    [Fact]
    public void ParseTargets_WithMultipleTargets_ShouldReturnAll()
    {
        var (success, tools, error) = TargetToolExtensions.ParseTargets("copilot,claude,cursor");

        success.Should().BeTrue();
        tools.Should().HaveCount(3);
        tools.Should().Contain(TargetTool.Copilot);
        tools.Should().Contain(TargetTool.Claude);
        tools.Should().Contain(TargetTool.Cursor);
        error.Should().BeNull();
    }

    [Fact]
    public void ParseTargets_CaseInsensitive_ShouldSucceed()
    {
        var (success, tools, _) = TargetToolExtensions.ParseTargets("COPILOT,Claude,CuRsOr");

        success.Should().BeTrue();
        tools.Should().HaveCount(3);
    }

    [Fact]
    public void ParseTargets_WithDuplicates_ShouldReturnDistinct()
    {
        var (success, tools, _) = TargetToolExtensions.ParseTargets("copilot,copilot,claude");

        success.Should().BeTrue();
        tools.Should().HaveCount(2);
    }

    [Fact]
    public void ParseTargets_WithUnknownTarget_ShouldFail()
    {
        var (success, tools, error) = TargetToolExtensions.ParseTargets("unknown");

        success.Should().BeFalse();
        tools.Should().BeNull();
        error.Should().Contain("Unknown target tool");
        error.Should().Contain("unknown");
    }

    [Fact]
    public void ParseTargets_WithEmptyString_ShouldFail()
    {
        var (success, tools, error) = TargetToolExtensions.ParseTargets("");

        success.Should().BeFalse();
        error.Should().Contain("No valid target tools");
    }

    [Fact]
    public void ParseTargets_WithSpacesInInput_ShouldTrim()
    {
        var (success, tools, _) = TargetToolExtensions.ParseTargets("copilot , claude , cursor");

        success.Should().BeTrue();
        tools.Should().HaveCount(3);
    }

    [Theory]
    [InlineData(TargetTool.Copilot, "copilot")]
    [InlineData(TargetTool.Claude, "claude")]
    [InlineData(TargetTool.Cursor, "cursor")]
    [InlineData(TargetTool.Windsurf, "windsurf")]
    [InlineData(TargetTool.Cline, "cline")]
    [InlineData(TargetTool.Aider, "aider")]
    public void ToConfigName_ShouldReturnLowercaseName(TargetTool tool, string expected)
    {
        tool.ToConfigName().Should().Be(expected);
    }
}
