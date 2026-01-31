using DevTools.CopilotAssets.Services;
using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Parsing;

namespace DevTools.CopilotAssets.Tests.Commands;

public class DoctorCommandTests
{
    private readonly Mock<IPolicyAppService> _mockAppService;
    private readonly TestConsole _console;

    public DoctorCommandTests()
    {
        _mockAppService = new Mock<IPolicyAppService>();
        _console = new TestConsole();
    }

    [Fact]
    public async Task Doctor_ShouldCallDiagnoseAsync()
    {
        // Arrange
        var result = new DiagnosticsResult
        {
            ToolVersion = "1.0.0",
            GitAvailable = true,
            IsGitRepository = true,
            ManifestExists = true
        };
        _mockAppService.Setup(s => s.DiagnoseAsync())
            .ReturnsAsync(result);

        var command = CreateDoctorCommand();

        // Act
        await command.InvokeAsync([], _console);

        // Assert
        _mockAppService.Verify(s => s.DiagnoseAsync(), Times.Once);
    }

    [Fact]
    public async Task Doctor_WhenNoIssues_ShouldReturnZero()
    {
        // Arrange
        var result = new DiagnosticsResult
        {
            ToolVersion = "1.0.0",
            GitAvailable = true,
            IsGitRepository = true,
            ManifestExists = true
        };
        _mockAppService.Setup(s => s.DiagnoseAsync())
            .ReturnsAsync(result);

        var command = CreateDoctorCommand();

        // Act
        var exitCode = await command.InvokeAsync([], _console);

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task Doctor_WhenHasIssues_ShouldReturnOne()
    {
        // Arrange
        var result = new DiagnosticsResult
        {
            ToolVersion = "1.0.0",
            GitAvailable = false,
            IsGitRepository = false,
            ManifestExists = false
        };
        result.Issues.Add("Git is not available");
        _mockAppService.Setup(s => s.DiagnoseAsync())
            .ReturnsAsync(result);

        var command = CreateDoctorCommand();

        // Act
        var exitCode = await command.InvokeAsync([], _console);

        // Assert
        exitCode.Should().Be(1);
    }

    private Command CreateDoctorCommand()
    {
        var doctorCommand = new Command("doctor", "Check the system for common issues");

        doctorCommand.SetHandler(async (context) =>
        {
            var result = await _mockAppService.Object.DiagnoseAsync();
            context.ExitCode = result.HasIssues ? 1 : 0;
        });

        return doctorCommand;
    }
}
