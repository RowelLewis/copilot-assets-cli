using DevTools.CopilotAssets.Domain;
using DevTools.CopilotAssets.Services;
using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Parsing;

namespace DevTools.CopilotAssets.Tests.Commands;

public class UpdateCommandTests
{
    private readonly Mock<IPolicyAppService> _mockAppService;
    private readonly TestConsole _console;

    public UpdateCommandTests()
    {
        _mockAppService = new Mock<IPolicyAppService>();
        _console = new TestConsole();
    }

    [Fact]
    public async Task Update_WithDefaults_ShouldCallUpdateAsync()
    {
        // Arrange
        var result = ValidationResult.Success();
        _mockAppService.Setup(s => s.UpdateAsync(It.IsAny<UpdateOptions>()))
            .ReturnsAsync(result);

        var command = CreateUpdateCommand();

        // Act
        await command.InvokeAsync([], _console);

        // Assert
        _mockAppService.Verify(s => s.UpdateAsync(It.Is<UpdateOptions>(o =>
            o.TargetDirectory == "." &&
            o.Force == false)), Times.Once);
    }

    [Fact]
    public async Task Update_WithForceFlag_ShouldPassForceOption()
    {
        // Arrange
        var result = ValidationResult.Success();
        _mockAppService.Setup(s => s.UpdateAsync(It.IsAny<UpdateOptions>()))
            .ReturnsAsync(result);

        var command = CreateUpdateCommand();

        // Act
        await command.InvokeAsync(["--force"], _console);

        // Assert
        _mockAppService.Verify(s => s.UpdateAsync(It.Is<UpdateOptions>(o =>
            o.Force == true)), Times.Once);
    }

    [Fact]
    public async Task Update_WhenSuccess_ShouldReturnZero()
    {
        // Arrange
        var result = ValidationResult.Success();
        _mockAppService.Setup(s => s.UpdateAsync(It.IsAny<UpdateOptions>()))
            .ReturnsAsync(result);

        var command = CreateUpdateCommand();

        // Act
        var exitCode = await command.InvokeAsync([], _console);

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task Update_WhenFails_ShouldReturnOne()
    {
        // Arrange
        var result = ValidationResult.Failure("Update failed");
        _mockAppService.Setup(s => s.UpdateAsync(It.IsAny<UpdateOptions>()))
            .ReturnsAsync(result);

        var command = CreateUpdateCommand();

        // Act
        var exitCode = await command.InvokeAsync([], _console);

        // Assert
        exitCode.Should().Be(1);
    }

    private Command CreateUpdateCommand()
    {
        var updateCommand = new Command("update", "Update Copilot assets to the latest version");

        var directoryOption = new Option<string>(
            aliases: ["-d", "--directory"],
            getDefaultValue: () => ".",
            description: "Target directory");

        var forceOption = new Option<bool>(
            aliases: ["-f", "--force"],
            description: "Force update even if already latest");

        updateCommand.AddOption(directoryOption);
        updateCommand.AddOption(forceOption);

        updateCommand.SetHandler(async (context) =>
        {
            var directory = context.ParseResult.GetValueForOption(directoryOption)!;
            var force = context.ParseResult.GetValueForOption(forceOption);

            var options = new UpdateOptions
            {
                TargetDirectory = directory,
                Force = force
            };

            var result = await _mockAppService.Object.UpdateAsync(options);
            context.ExitCode = result.IsCompliant ? 0 : 1;
        });

        return updateCommand;
    }
}
