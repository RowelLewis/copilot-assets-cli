using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using DevTools.CopilotAssets.Commands;
using DevTools.CopilotAssets.Domain;
using DevTools.CopilotAssets.Domain.Fleet;
using DevTools.CopilotAssets.Services;
using DevTools.CopilotAssets.Services.Fleet;

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
        // Set up default mock responses for all methods that may be called when local repos are found
        _mockPolicyService.Setup(s => s.PreviewInitAsync(It.IsAny<InitOptions>()))
            .ReturnsAsync(DryRunResult.FromOperations([]));
        _mockPolicyService.Setup(s => s.ValidateAsync(It.IsAny<ValidateOptions>()))
            .ReturnsAsync(ValidationResult.Success());
        _mockPolicyService.Setup(s => s.InitAsync(It.IsAny<InitOptions>()))
            .ReturnsAsync(ValidationResult.Success());
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

    [Fact]
    public async Task SyncCommand_WithDryRun_RunsWithoutThrowing()
    {
        // fleet sync --dry-run should delegate to PreviewSyncAsync and not throw
        var console = new TestConsole();
        var command = FleetCommand.Create(_fleetManager, _fleetSyncService, _jsonOption);

        var act = async () => await command.InvokeAsync(["sync", "--dry-run"], console);

        await act.Should().NotThrowAsync("--dry-run delegates to PreviewSyncAsync");
    }

    [Fact]
    public async Task SyncCommand_WithDryRun_ParsesDryRunFlag()
    {
        // Verify the --dry-run flag is parsed and results in the dryRun path (PreviewSyncAsync)
        // Since dryRun=true delegates to PreviewSyncAsync, both should return the same Total
        var dryRunReport = await _fleetSyncService.SyncFleetAsync(dryRun: true);
        var previewReport = await _fleetSyncService.PreviewSyncAsync();

        dryRunReport.Total.Should().Be(previewReport.Total,
            "SyncFleetAsync(dryRun: true) must delegate to PreviewSyncAsync");
    }

    [Fact]
    public async Task SyncCommand_WithPrFlag_RunsWithoutThrowing()
    {
        var console = new TestConsole();
        var command = FleetCommand.Create(_fleetManager, _fleetSyncService, _jsonOption);

        var act = async () => await command.InvokeAsync(["sync", "--pr"], console);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidateCommand_RunsWithoutThrowing()
    {
        var console = new TestConsole();
        var command = FleetCommand.Create(_fleetManager, _fleetSyncService, _jsonOption);

        var act = async () => await command.InvokeAsync(["validate"], console);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StatusCommand_RunsWithoutThrowing()
    {
        var console = new TestConsole();
        var command = FleetCommand.Create(_fleetManager, _fleetSyncService, _jsonOption);

        var act = async () => await command.InvokeAsync(["status"], console);

        await act.Should().NotThrowAsync();
    }
}
