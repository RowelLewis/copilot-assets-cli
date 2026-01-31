using DevTools.CopilotAssets.Domain;
using DevTools.CopilotAssets.Services;
using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Parsing;

namespace DevTools.CopilotAssets.Tests.Commands;

public class InitCommandTests
{
    private readonly Mock<IPolicyAppService> _mockAppService;
    private readonly TestConsole _console;

    public InitCommandTests()
    {
        _mockAppService = new Mock<IPolicyAppService>();
        _console = new TestConsole();
    }

    [Fact]
    public async Task Init_WithDefaults_ShouldCallInitAsync()
    {
        // Arrange
        var result = ValidationResult.Success();
        _mockAppService.Setup(s => s.InitAsync(It.IsAny<InitOptions>()))
            .ReturnsAsync(result);

        var command = CreateInitCommand();

        // Act
        var exitCode = await command.InvokeAsync([], _console);

        // Assert
        _mockAppService.Verify(s => s.InitAsync(It.Is<InitOptions>(o =>
            o.TargetDirectory == "." &&
            o.Force == false &&
            o.NoGit == false)), Times.Once);
    }

    [Fact]
    public async Task Init_WithForceFlag_ShouldPassForceOption()
    {
        // Arrange
        var result = ValidationResult.Success();
        _mockAppService.Setup(s => s.InitAsync(It.IsAny<InitOptions>()))
            .ReturnsAsync(result);

        var command = CreateInitCommand();

        // Act
        await command.InvokeAsync(["--force"], _console);

        // Assert
        _mockAppService.Verify(s => s.InitAsync(It.Is<InitOptions>(o => o.Force == true)), Times.Once);
    }

    [Fact]
    public async Task Init_WithNoGitFlag_ShouldPassNoGitOption()
    {
        // Arrange
        var result = ValidationResult.Success();
        _mockAppService.Setup(s => s.InitAsync(It.IsAny<InitOptions>()))
            .ReturnsAsync(result);

        var command = CreateInitCommand();

        // Act
        await command.InvokeAsync(["--no-git"], _console);

        // Assert
        _mockAppService.Verify(s => s.InitAsync(It.Is<InitOptions>(o => o.NoGit == true)), Times.Once);
    }

    [Fact]
    public async Task Init_WithTargetDirectory_ShouldPassDirectory()
    {
        // Arrange
        var result = ValidationResult.Success();
        _mockAppService.Setup(s => s.InitAsync(It.IsAny<InitOptions>()))
            .ReturnsAsync(result);

        var command = CreateInitCommand();

        // Act
        await command.InvokeAsync(["-d", "/custom/path"], _console);

        // Assert
        _mockAppService.Verify(s => s.InitAsync(It.Is<InitOptions>(o =>
            o.TargetDirectory == "/custom/path")), Times.Once);
    }

    [Fact]
    public async Task Init_WithSuccess_ShouldReturnZero()
    {
        // Arrange
        var result = ValidationResult.Success();
        _mockAppService.Setup(s => s.InitAsync(It.IsAny<InitOptions>()))
            .ReturnsAsync(result);

        var command = CreateInitCommand();

        // Act
        var exitCode = await command.InvokeAsync([], _console);

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task Init_WithErrors_ShouldReturnOne()
    {
        // Arrange
        var result = ValidationResult.Failure("Some error");
        _mockAppService.Setup(s => s.InitAsync(It.IsAny<InitOptions>()))
            .ReturnsAsync(result);

        var command = CreateInitCommand();

        // Act
        var exitCode = await command.InvokeAsync([], _console);

        // Assert
        exitCode.Should().Be(1);
    }

    private Command CreateInitCommand()
    {
        var initCommand = new Command("init", "Initialize Copilot assets in the target directory");

        var directoryOption = new Option<string>(
            aliases: ["-d", "--directory"],
            getDefaultValue: () => ".",
            description: "Target directory");

        var forceOption = new Option<bool>(
            aliases: ["-f", "--force"],
            description: "Force overwrite existing files");

        var noGitOption = new Option<bool>(
            aliases: ["--no-git"],
            description: "Skip Git operations");

        initCommand.AddOption(directoryOption);
        initCommand.AddOption(forceOption);
        initCommand.AddOption(noGitOption);

        initCommand.SetHandler(async (context) =>
        {
            var directory = context.ParseResult.GetValueForOption(directoryOption)!;
            var force = context.ParseResult.GetValueForOption(forceOption);
            var noGit = context.ParseResult.GetValueForOption(noGitOption);

            var options = new InitOptions
            {
                TargetDirectory = directory,
                Force = force,
                NoGit = noGit
            };

            var result = await _mockAppService.Object.InitAsync(options);
            context.ExitCode = result.IsCompliant ? 0 : 1;
        });

        return initCommand;
    }
}
