using DevTools.CopilotAssets.Domain;
using DevTools.CopilotAssets.Services;
using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Parsing;

namespace DevTools.CopilotAssets.Tests.Commands;

public class ValidateCommandTests
{
    private readonly Mock<IPolicyAppService> _mockAppService;
    private readonly TestConsole _console;

    public ValidateCommandTests()
    {
        _mockAppService = new Mock<IPolicyAppService>();
        _console = new TestConsole();
    }

    [Fact]
    public async Task Validate_WithDefaults_ShouldCallValidateAsync()
    {
        // Arrange
        var result = ValidationResult.Success();
        _mockAppService.Setup(s => s.ValidateAsync(It.IsAny<ValidateOptions>()))
            .ReturnsAsync(result);

        var command = CreateValidateCommand();

        // Act
        await command.InvokeAsync([], _console);

        // Assert
        _mockAppService.Verify(s => s.ValidateAsync(It.Is<ValidateOptions>(o =>
            o.TargetDirectory == "." &&
            o.CiMode == false)), Times.Once);
    }

    [Fact]
    public async Task Validate_WithCiMode_ShouldPassCiModeOption()
    {
        // Arrange
        var result = ValidationResult.Success();
        _mockAppService.Setup(s => s.ValidateAsync(It.IsAny<ValidateOptions>()))
            .ReturnsAsync(result);

        var command = CreateValidateCommand();

        // Act
        await command.InvokeAsync(["--ci"], _console);

        // Assert
        _mockAppService.Verify(s => s.ValidateAsync(It.Is<ValidateOptions>(o =>
            o.CiMode == true)), Times.Once);
    }

    [Fact]
    public async Task Validate_WhenCompliant_ShouldReturnZero()
    {
        // Arrange
        var result = ValidationResult.Success();
        _mockAppService.Setup(s => s.ValidateAsync(It.IsAny<ValidateOptions>()))
            .ReturnsAsync(result);

        var command = CreateValidateCommand();

        // Act
        var exitCode = await command.InvokeAsync([], _console);

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task Validate_WhenNotCompliant_ShouldReturnOne()
    {
        // Arrange
        var result = ValidationResult.Failure("Validation error");
        _mockAppService.Setup(s => s.ValidateAsync(It.IsAny<ValidateOptions>()))
            .ReturnsAsync(result);

        var command = CreateValidateCommand();

        // Act
        var exitCode = await command.InvokeAsync([], _console);

        // Assert
        exitCode.Should().Be(1);
    }

    [Fact]
    public async Task Validate_InCiMode_WhenNotCompliant_ShouldReturnNonZero()
    {
        // Arrange
        var result = ValidationResult.Failure("CI validation error");
        _mockAppService.Setup(s => s.ValidateAsync(It.IsAny<ValidateOptions>()))
            .ReturnsAsync(result);

        var command = CreateValidateCommand();

        // Act
        var exitCode = await command.InvokeAsync(["--ci"], _console);

        // Assert
        exitCode.Should().NotBe(0);
    }

    private Command CreateValidateCommand()
    {
        var validateCommand = new Command("validate", "Validate Copilot assets in the target directory");

        var directoryOption = new Option<string>(
            aliases: ["-d", "--directory"],
            getDefaultValue: () => ".",
            description: "Target directory");

        var ciModeOption = new Option<bool>(
            aliases: ["--ci"],
            description: "Run in CI mode (stricter validation)");

        validateCommand.AddOption(directoryOption);
        validateCommand.AddOption(ciModeOption);

        validateCommand.SetHandler(async (context) =>
        {
            var directory = context.ParseResult.GetValueForOption(directoryOption)!;
            var ciMode = context.ParseResult.GetValueForOption(ciModeOption);

            var options = new ValidateOptions
            {
                TargetDirectory = directory,
                CiMode = ciMode
            };

            var result = await _mockAppService.Object.ValidateAsync(options);
            context.ExitCode = result.IsCompliant ? 0 : 1;
        });

        return validateCommand;
    }
}
