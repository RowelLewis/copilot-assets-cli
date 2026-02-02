using DevTools.CopilotAssets.Commands;
using DevTools.CopilotAssets.Domain.Configuration;
using FluentAssertions;
using Xunit;

namespace DevTools.CopilotAssets.Tests.Commands;

public class ConfigCommandTests
{
    [Fact]
    public void Create_ShouldReturnCommandWithSubcommands()
    {
        // Arrange
        var jsonOption = new System.CommandLine.Option<bool>("--json");

        // Act
        var command = ConfigCommand.Create(jsonOption);

        // Assert
        command.Should().NotBeNull();
        command.Name.Should().Be("config");
        command.Subcommands.Should().HaveCount(4); // get, set, list, reset
    }

    [Fact]
    public void SubCommands_ShouldHaveCorrectNames()
    {
        // Arrange
        var jsonOption = new System.CommandLine.Option<bool>("--json");

        // Act
        var command = ConfigCommand.Create(jsonOption);

        // Assert
        var subcommandNames = command.Subcommands.Select(c => c.Name).ToList();
        subcommandNames.Should().Contain("get");
        subcommandNames.Should().Contain("set");
        subcommandNames.Should().Contain("list");
        subcommandNames.Should().Contain("reset");
    }

    [Fact]
    public void GetCommand_ShouldHaveKeyArgument()
    {
        // Arrange
        var jsonOption = new System.CommandLine.Option<bool>("--json");

        // Act
        var command = ConfigCommand.Create(jsonOption);
        var getCommand = command.Subcommands.First(c => c.Name == "get");

        // Assert
        getCommand.Arguments.Should().HaveCount(1);
        getCommand.Arguments[0].Name.Should().Be("key");
    }

    [Fact]
    public void SetCommand_ShouldHaveKeyAndValueArguments()
    {
        // Arrange
        var jsonOption = new System.CommandLine.Option<bool>("--json");

        // Act
        var command = ConfigCommand.Create(jsonOption);
        var setCommand = command.Subcommands.First(c => c.Name == "set");

        // Assert
        setCommand.Arguments.Should().HaveCount(2);
        setCommand.Arguments[0].Name.Should().Be("key");
        setCommand.Arguments[1].Name.Should().Be("value");
    }
}
