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

    [Fact]
    public void AddRepo_WithInvalidFormat_ShouldThrowArgumentException()
    {
        var manager = new FleetManager();

        var act = () => manager.AddRepo("not-valid-format");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*not-valid-format*");
    }

    [Fact]
    public void AddRepo_WithMissingOwner_ShouldThrowArgumentException()
    {
        var manager = new FleetManager();

        var act = () => manager.AddRepo("onlynamewithoutslash");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RemoveRepo_WhenRepoNotInFleet_ShouldThrowInvalidOperationException()
    {
        var manager = new FleetManager();

        // Use a repo name that's extremely unlikely to be in anyone's actual fleet config
        var act = () => manager.RemoveRepo("does-not-exist/repo-xyzzy-12345");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*does-not-exist/repo-xyzzy-12345*");
    }

    [Fact]
    public void ListRepos_ShouldReturnReadOnlyList()
    {
        var manager = new FleetManager();

        var result = manager.ListRepos();

        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IReadOnlyList<FleetRepo>>();
    }

    [Fact]
    public void GetEffectiveSource_WithCustomDefaultSource_ShouldReturnCustomDefault()
    {
        var config = new FleetConfig
        {
            Defaults = new FleetDefaults { Source = "enterprise-pack" }
        };
        var repo = new FleetRepo { Name = "org/repo" };

        var result = FleetManager.GetEffectiveSource(repo, config);

        result.Should().Be("enterprise-pack");
    }

    [Fact]
    public void GetEffectiveTargets_WithCustomDefaultTargets_ShouldReturnCustomDefaults()
    {
        var config = new FleetConfig
        {
            Defaults = new FleetDefaults { Targets = ["copilot", "claude"] }
        };
        var repo = new FleetRepo { Name = "org/repo" };

        var result = FleetManager.GetEffectiveTargets(repo, config);

        result.Should().HaveCount(2);
        result.Should().Contain("claude");
    }

    [Fact]
    public void GetEffectiveBranch_WithCustomDefaultBranch_ShouldReturnCustomDefault()
    {
        var config = new FleetConfig
        {
            Defaults = new FleetDefaults { Branch = "develop" }
        };
        var repo = new FleetRepo { Name = "org/repo" };

        var result = FleetManager.GetEffectiveBranch(repo, config);

        result.Should().Be("develop");
    }
}
