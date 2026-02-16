using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Text;
using System.Text.Json;
using DevTools.CopilotAssets.Commands;
using DevTools.CopilotAssets.Services.Http;
using DevTools.CopilotAssets.Services.Registry;

namespace DevTools.CopilotAssets.Tests.Commands;

public class RegistryCommandTests
{
    private readonly RegistryClient _registryClient;
    private readonly System.CommandLine.Option<bool> _jsonOption;

    public RegistryCommandTests()
    {
        _registryClient = new RegistryClient(new GitHubClient());
        _jsonOption = new System.CommandLine.Option<bool>("--json");
    }

    [Fact]
    public void Create_ShouldReturnCommandWithCorrectName()
    {
        var command = RegistryCommand.Create(_registryClient, _jsonOption);

        command.Should().NotBeNull();
        command.Name.Should().Be("registry");
    }

    [Fact]
    public void Create_ShouldHaveFiveSubcommands()
    {
        var command = RegistryCommand.Create(_registryClient, _jsonOption);

        command.Subcommands.Should().HaveCount(5);
    }

    [Fact]
    public void Subcommands_ShouldHaveAllExpectedNames()
    {
        var command = RegistryCommand.Create(_registryClient, _jsonOption);

        var names = command.Subcommands.Select(c => c.Name).ToList();
        names.Should().Contain("search");
        names.Should().Contain("info");
        names.Should().Contain("install");
        names.Should().Contain("list");
        names.Should().Contain("publish");
    }

    [Fact]
    public void SearchCommand_ShouldHaveQueryArgument()
    {
        var command = RegistryCommand.Create(_registryClient, _jsonOption);
        var searchCommand = command.Subcommands.First(c => c.Name == "search");

        searchCommand.Arguments.Should().HaveCount(1);
        searchCommand.Arguments[0].Name.Should().Be("query");
    }

    [Fact]
    public void InfoCommand_ShouldHaveNameArgument()
    {
        var command = RegistryCommand.Create(_registryClient, _jsonOption);
        var infoCommand = command.Subcommands.First(c => c.Name == "info");

        infoCommand.Arguments.Should().HaveCount(1);
        infoCommand.Arguments[0].Name.Should().Be("name");
    }

    [Fact]
    public void InstallCommand_ShouldHaveNameArgument()
    {
        var command = RegistryCommand.Create(_registryClient, _jsonOption);
        var installCommand = command.Subcommands.First(c => c.Name == "install");

        installCommand.Arguments.Should().HaveCount(1);
        installCommand.Arguments[0].Name.Should().Be("name");
    }

    [Fact]
    public void ListCommand_ShouldHaveNoRequiredArguments()
    {
        var command = RegistryCommand.Create(_registryClient, _jsonOption);
        var listCommand = command.Subcommands.First(c => c.Name == "list");

        listCommand.Arguments.Should().BeEmpty();
    }

    [Fact]
    public void PublishCommand_ShouldHavePathArgumentAndSubmitOption()
    {
        var command = RegistryCommand.Create(_registryClient, _jsonOption);
        var publishCommand = command.Subcommands.First(c => c.Name == "publish");

        publishCommand.Arguments.Should().HaveCount(1);
        publishCommand.Arguments[0].Name.Should().Be("path");

        var optionNames = publishCommand.Options.Select(o => o.Name).ToList();
        optionNames.Should().Contain("submit");
    }

    [Fact]
    public async Task PublishCommand_ValidPackJson_ReturnsExitCodeZero()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var pack = new
            {
                name = "my-test-pack",
                description = "A test pack",
                author = "test-author",
                repo = "testorg/test-pack",
                targets = new[] { "copilot" },
                version = "1.0.0"
            };
            await File.WriteAllTextAsync(
                Path.Combine(tempDir, "pack.json"),
                JsonSerializer.Serialize(pack));

            var console = new TestConsole();
            var command = RegistryCommand.Create(_registryClient, _jsonOption);

            // Act
            var exitCode = await command.InvokeAsync(["publish", tempDir], console);

            // Assert
            exitCode.Should().Be(0, "valid pack.json should succeed");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task PublishCommand_MissingPackJson_ReturnsExitCodeOne()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var console = new TestConsole();
            var command = RegistryCommand.Create(_registryClient, _jsonOption);

            // Act - no pack.json in the temp dir
            var exitCode = await command.InvokeAsync(["publish", tempDir], console);

            // Assert: missing pack.json should return exit code 1
            // Note: error text goes to Console.Error (not TestConsole), so we only check exit code
            exitCode.Should().Be(1, "missing pack.json should fail");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task PublishCommand_MissingRequiredFields_ReturnsExitCodeOne()
    {
        // Arrange - pack.json with missing required fields
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            // Missing: description, author, repo, targets
            var pack = new { name = "incomplete-pack" };
            await File.WriteAllTextAsync(
                Path.Combine(tempDir, "pack.json"),
                JsonSerializer.Serialize(pack));

            var console = new TestConsole();
            var command = RegistryCommand.Create(_registryClient, _jsonOption);

            // Act
            var exitCode = await command.InvokeAsync(["publish", tempDir], console);

            // Assert
            exitCode.Should().Be(1, "pack with missing required fields should fail validation");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task PublishCommand_JsonOutput_ReturnsExitCodeZero()
    {
        // Arrange: --json is a global option on the root command; we simulate that here
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var pack = new
            {
                name = "json-test-pack",
                description = "A test pack for JSON output",
                author = "test-author",
                repo = "testorg/json-test-pack",
                targets = new[] { "copilot" },
                version = "1.0.0"
            };
            await File.WriteAllTextAsync(
                Path.Combine(tempDir, "pack.json"),
                JsonSerializer.Serialize(pack));

            var jsonOption = new Option<bool>("--json");
            var registryCommand = RegistryCommand.Create(_registryClient, jsonOption);

            // Create root command with global --json option (mirrors production Program.cs setup)
            var rootCommand = new RootCommand();
            rootCommand.AddGlobalOption(jsonOption);
            rootCommand.AddCommand(registryCommand);

            var console = new TestConsole();

            // Act: invoke as "registry publish <path> --json" from root
            var exitCode = await rootCommand.InvokeAsync(["registry", "publish", tempDir, "--json"], console);

            // Assert: valid pack should succeed even with --json flag
            exitCode.Should().Be(0, "valid pack.json with --json should succeed");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
