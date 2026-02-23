using DevTools.CopilotAssets.Services.Http;
using DevTools.CopilotAssets.Services.Registry;
using FluentAssertions;

namespace DevTools.CopilotAssets.Tests.Services.Registry;

public class RegistryClientTests
{
    private static readonly string TestIndexJson = """
        {
          "version": 1,
          "packs": [
            {
              "name": "dotnet-enterprise",
              "description": "Enterprise .NET coding standards",
              "author": "DevTools Team",
              "repo": "copilot-assets/pack-dotnet-enterprise",
              "tags": ["dotnet", "csharp", "enterprise"],
              "targets": ["copilot", "claude", "cursor"],
              "version": "1.0.0"
            },
            {
              "name": "react-starter",
              "description": "React best practices and patterns",
              "author": "Community",
              "repo": "community/react-starter-pack",
              "tags": ["react", "javascript", "frontend"],
              "targets": ["copilot", "cursor"],
              "version": "2.1.0"
            },
            {
              "name": "python-ml",
              "description": "Machine learning Python conventions",
              "author": "ML Team",
              "repo": "ml-team/python-ml-pack",
              "tags": ["python", "ml", "data-science"],
              "targets": ["copilot", "claude"],
              "version": "1.2.0"
            }
          ]
        }
        """;

    private RegistryClient CreateClientWithTestData()
    {
        var client = new RegistryClient(new GitHubClient());
        client.LoadFromJson(TestIndexJson);
        return client;
    }

    [Fact]
    public async Task SearchAsync_ByName_ShouldFindMatch()
    {
        var client = CreateClientWithTestData();

        var results = await client.SearchAsync("dotnet");

        results.Should().ContainSingle();
        results[0].Name.Should().Be("dotnet-enterprise");
    }

    [Fact]
    public async Task SearchAsync_ByTag_ShouldFindMatch()
    {
        var client = CreateClientWithTestData();

        var results = await client.SearchAsync("react");

        results.Should().ContainSingle();
        results[0].Name.Should().Be("react-starter");
    }

    [Fact]
    public async Task SearchAsync_ByDescription_ShouldFindMatch()
    {
        var client = CreateClientWithTestData();

        var results = await client.SearchAsync("machine learning");

        results.Should().ContainSingle();
        results[0].Name.Should().Be("python-ml");
    }

    [Fact]
    public async Task SearchAsync_CaseInsensitive_ShouldFindMatch()
    {
        var client = CreateClientWithTestData();

        var results = await client.SearchAsync("DOTNET");

        results.Should().ContainSingle();
    }

    [Fact]
    public async Task SearchAsync_NoMatch_ShouldReturnEmpty()
    {
        var client = CreateClientWithTestData();

        var results = await client.SearchAsync("nonexistent");

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPackAsync_ExistingPack_ShouldReturnPack()
    {
        var client = CreateClientWithTestData();

        var pack = await client.GetPackAsync("react-starter");

        pack.Should().NotBeNull();
        pack!.Repo.Should().Be("community/react-starter-pack");
        pack.Version.Should().Be("2.1.0");
        pack.Author.Should().Be("Community");
    }

    [Fact]
    public async Task GetPackAsync_NonExistent_ShouldReturnNull()
    {
        var client = CreateClientWithTestData();

        var pack = await client.GetPackAsync("unknown-pack");

        pack.Should().BeNull();
    }

    [Fact]
    public async Task ListAsync_ShouldReturnAllPacks()
    {
        var client = CreateClientWithTestData();

        var packs = await client.ListAsync();

        packs.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetPackAsync_CaseInsensitive_ShouldFindMatch()
    {
        var client = CreateClientWithTestData();

        var pack = await client.GetPackAsync("DOTNET-ENTERPRISE");

        pack.Should().NotBeNull();
    }
}
