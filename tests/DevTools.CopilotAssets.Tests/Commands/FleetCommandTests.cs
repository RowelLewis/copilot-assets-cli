using DevTools.CopilotAssets.Commands;
using DevTools.CopilotAssets.Services.Fleet;
using DevTools.CopilotAssets.Services;
using FluentAssertions;
using Xunit;

namespace DevTools.CopilotAssets.Tests.Commands;

public class FleetCommandTests
{
    private readonly Mock<IPolicyAppService> _mockPolicyService;
    private readonly FleetManager _fleetManager;
    private readonly FleetSyncService _fleetSyncService;
    private readonly System.CommandLine.Option<bool> _jsonOption;

    public FleetCommandTests()
    {
        _mockPolicyService = new Mock<IPolicyAppService>();
        _fleetManager = new FleetManager();
        _fleetSyncService = new FleetSyncService(_mockPolicyService.Object);
        _jsonOption = new System.CommandLine.Option<bool>("--json");
    }

    [Fact]
    public void Create_ShouldReturnCommandWithCorrectName()
    {
        var command = FleetCommand.Create(_fleetManager, _fleetSyncService, _jsonOption);

        command.Should().NotBeNull();
        command.Name.Should().Be("fleet");
    }

    [Fact]
    public void Create_ShouldHaveSixSubcommands()
    {
        var command = FleetCommand.Create(_fleetManager, _fleetSyncService, _jsonOption);

        command.Subcommands.Should().HaveCount(6);
    }

    [Fact]
    public void Subcommands_ShouldHaveAllExpectedNames()
    {
        var command = FleetCommand.Create(_fleetManager, _fleetSyncService, _jsonOption);

        var names = command.Subcommands.Select(c => c.Name).ToList();
        names.Should().Contain("add");
        names.Should().Contain("remove");
        names.Should().Contain("list");
        names.Should().Contain("sync");
        names.Should().Contain("validate");
        names.Should().Contain("status");
    }

    [Fact]
    public void AddCommand_ShouldHaveRepoArgument()
    {
        var command = FleetCommand.Create(_fleetManager, _fleetSyncService, _jsonOption);
        var addCommand = command.Subcommands.First(c => c.Name == "add");

        addCommand.Arguments.Should().HaveCount(1);
        addCommand.Arguments[0].Name.Should().Be("repo");
    }

    [Fact]
    public void AddCommand_ShouldHaveSourceTargetBranchOptions()
    {
        var command = FleetCommand.Create(_fleetManager, _fleetSyncService, _jsonOption);
        var addCommand = command.Subcommands.First(c => c.Name == "add");

        var optionNames = addCommand.Options.Select(o => o.Name).ToList();
        optionNames.Should().Contain("source");
        optionNames.Should().Contain("target");
        optionNames.Should().Contain("branch");
    }

    [Fact]
    public void RemoveCommand_ShouldHaveRepoArgument()
    {
        var command = FleetCommand.Create(_fleetManager, _fleetSyncService, _jsonOption);
        var removeCommand = command.Subcommands.First(c => c.Name == "remove");

        removeCommand.Arguments.Should().HaveCount(1);
        removeCommand.Arguments[0].Name.Should().Be("repo");
    }

    [Fact]
    public void SyncCommand_ShouldHaveDryRunAndPrOptions()
    {
        var command = FleetCommand.Create(_fleetManager, _fleetSyncService, _jsonOption);
        var syncCommand = command.Subcommands.First(c => c.Name == "sync");

        var optionNames = syncCommand.Options.Select(o => o.Name).ToList();
        optionNames.Should().Contain("dry-run");
        optionNames.Should().Contain("pr");
    }

    [Fact]
    public void ValidateCommand_ShouldHaveNoRequiredArguments()
    {
        var command = FleetCommand.Create(_fleetManager, _fleetSyncService, _jsonOption);
        var validateCommand = command.Subcommands.First(c => c.Name == "validate");

        validateCommand.Arguments.Should().BeEmpty();
    }

    [Fact]
    public void StatusCommand_ShouldHaveNoRequiredArguments()
    {
        var command = FleetCommand.Create(_fleetManager, _fleetSyncService, _jsonOption);
        var statusCommand = command.Subcommands.First(c => c.Name == "status");

        statusCommand.Arguments.Should().BeEmpty();
    }
}
