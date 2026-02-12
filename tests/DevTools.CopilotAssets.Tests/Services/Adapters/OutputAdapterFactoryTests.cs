using DevTools.CopilotAssets.Domain;
using DevTools.CopilotAssets.Services.Adapters;
using FluentAssertions;

namespace DevTools.CopilotAssets.Tests.Services.Adapters;

public class OutputAdapterFactoryTests
{
    private readonly OutputAdapterFactory _sut = new();

    [Theory]
    [InlineData(TargetTool.Copilot, typeof(CopilotOutputAdapter))]
    [InlineData(TargetTool.Claude, typeof(ClaudeOutputAdapter))]
    [InlineData(TargetTool.Cursor, typeof(CursorOutputAdapter))]
    [InlineData(TargetTool.Windsurf, typeof(WindsurfOutputAdapter))]
    [InlineData(TargetTool.Cline, typeof(ClineOutputAdapter))]
    [InlineData(TargetTool.Aider, typeof(AiderOutputAdapter))]
    public void CreateAdapter_ShouldReturnCorrectType(TargetTool target, Type expectedType)
    {
        var adapter = _sut.CreateAdapter(target);

        adapter.Should().BeOfType(expectedType);
        adapter.Target.Should().Be(target);
    }

    [Fact]
    public void CreateAdapters_ShouldReturnAllRequestedAdapters()
    {
        var targets = new[] { TargetTool.Copilot, TargetTool.Claude, TargetTool.Cursor };

        var adapters = _sut.CreateAdapters(targets);

        adapters.Should().HaveCount(3);
        adapters.Select(a => a.Target).Should().BeEquivalentTo(targets);
    }

    [Fact]
    public void CreateAdapters_WithDuplicates_ShouldReturnDistinct()
    {
        var targets = new[] { TargetTool.Copilot, TargetTool.Copilot, TargetTool.Claude };

        var adapters = _sut.CreateAdapters(targets);

        adapters.Should().HaveCount(2);
    }

    [Fact]
    public void AvailableTargets_ShouldContainAllTools()
    {
        var targets = OutputAdapterFactory.AvailableTargets;

        targets.Should().HaveCount(6);
        targets.Should().Contain(TargetTool.Copilot);
        targets.Should().Contain(TargetTool.Claude);
        targets.Should().Contain(TargetTool.Cursor);
        targets.Should().Contain(TargetTool.Windsurf);
        targets.Should().Contain(TargetTool.Cline);
        targets.Should().Contain(TargetTool.Aider);
    }
}
