using System.Text.Json;
using DevTools.CopilotAssets.Domain.Fleet;
using FluentAssertions;

namespace DevTools.CopilotAssets.Tests.Domain.Fleet;

public class FleetConfigTests
{
    [Fact]
    public void Default_ShouldHaveCorrectDefaults()
    {
        var config = new FleetConfig();

        config.Version.Should().Be(1);
        config.Repos.Should().BeEmpty();
        config.Defaults.Source.Should().Be("default");
        config.Defaults.Branch.Should().Be("main");
        config.Defaults.Targets.Should().ContainSingle().Which.Should().Be("copilot");
    }

    [Fact]
    public void Serialization_ShouldRoundTrip()
    {
        var config = new FleetConfig
        {
            Repos =
            [
                new FleetRepo
                {
                    Name = "org/repo1",
                    Source = "my-pack",
                    Targets = ["copilot", "claude"],
                    Branch = "develop"
                }
            ]
        };

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        var deserialized = JsonSerializer.Deserialize<FleetConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        deserialized.Should().NotBeNull();
        deserialized!.Repos.Should().HaveCount(1);
        deserialized.Repos[0].Name.Should().Be("org/repo1");
        deserialized.Repos[0].Source.Should().Be("my-pack");
        deserialized.Repos[0].Targets.Should().HaveCount(2);
        deserialized.Repos[0].Branch.Should().Be("develop");
    }
}
