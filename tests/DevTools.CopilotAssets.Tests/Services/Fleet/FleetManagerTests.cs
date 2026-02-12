using DevTools.CopilotAssets.Domain.Fleet;
using DevTools.CopilotAssets.Services.Fleet;
using FluentAssertions;

namespace DevTools.CopilotAssets.Tests.Services.Fleet;

public class FleetManagerTests
{
    [Fact]
    public void GetEffectiveSource_WithRepoSource_ShouldReturnRepoSource()
    {
        var config = new FleetConfig();
        var repo = new FleetRepo { Name = "org/repo", Source = "my-pack" };

        var result = FleetManager.GetEffectiveSource(repo, config);

        result.Should().Be("my-pack");
    }

    [Fact]
    public void GetEffectiveSource_WithoutRepoSource_ShouldReturnDefault()
    {
        var config = new FleetConfig();
        var repo = new FleetRepo { Name = "org/repo" };

        var result = FleetManager.GetEffectiveSource(repo, config);

        result.Should().Be("default");
    }

    [Fact]
    public void GetEffectiveTargets_WithRepoTargets_ShouldReturnRepoTargets()
    {
        var config = new FleetConfig();
        var repo = new FleetRepo
        {
            Name = "org/repo",
            Targets = ["copilot", "claude", "cursor"]
        };

        var result = FleetManager.GetEffectiveTargets(repo, config);

        result.Should().HaveCount(3);
        result.Should().Contain("claude");
    }

    [Fact]
    public void GetEffectiveTargets_WithoutRepoTargets_ShouldReturnDefault()
    {
        var config = new FleetConfig();
        var repo = new FleetRepo { Name = "org/repo" };

        var result = FleetManager.GetEffectiveTargets(repo, config);

        result.Should().ContainSingle().Which.Should().Be("copilot");
    }

    [Fact]
    public void GetEffectiveBranch_WithRepoBranch_ShouldReturnRepoBranch()
    {
        var config = new FleetConfig();
        var repo = new FleetRepo { Name = "org/repo", Branch = "develop" };

        var result = FleetManager.GetEffectiveBranch(repo, config);

        result.Should().Be("develop");
    }

    [Fact]
    public void GetEffectiveBranch_WithoutRepoBranch_ShouldReturnDefault()
    {
        var config = new FleetConfig();
        var repo = new FleetRepo { Name = "org/repo" };

        var result = FleetManager.GetEffectiveBranch(repo, config);

        result.Should().Be("main");
    }
}
