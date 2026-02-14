using DevTools.CopilotAssets.Commands;
using DevTools.CopilotAssets.Services.Http;
using DevTools.CopilotAssets.Services.Registry;
using FluentAssertions;
using Xunit;

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
}
