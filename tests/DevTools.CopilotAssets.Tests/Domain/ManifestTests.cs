using DevTools.CopilotAssets.Domain;

namespace DevTools.CopilotAssets.Tests.Domain;

public class ManifestTests
{
    [Fact]
    public void Create_ShouldInitializeWithCorrectValues()
    {
        // Arrange
        var toolVersion = "1.0.0.0";

        // Act
        var manifest = Manifest.Create(toolVersion);

        // Assert
        manifest.SchemaVersion.Should().Be(Manifest.CurrentSchemaVersion);
        manifest.ToolVersion.Should().Be(toolVersion);
        manifest.InstalledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        manifest.Assets.Should().BeEmpty();
        manifest.Checksums.Should().BeEmpty();
        manifest.Source.Type.Should().Be("default");
    }

    [Fact]
    public void Create_WithSource_ShouldSetSource()
    {
        // Arrange
        var toolVersion = "1.0.0.0";
        var source = TemplateSource.Remote("owner/repo", "main");

        // Act
        var manifest = Manifest.Create(toolVersion, source);

        // Assert
        manifest.Source.Type.Should().Be("remote");
        manifest.Source.Repo.Should().Be("owner/repo");
        manifest.Source.Branch.Should().Be("main");
    }

    [Fact]
    public void ToJson_ShouldSerializeCorrectly()
    {
        // Arrange
        var manifest = Manifest.Create("1.0.0.0");
        manifest.Assets.Add("copilot-instructions.md");
        manifest.Assets.Add("prompts/test.md");
        manifest.Checksums["copilot-instructions.md"] = "abc123";
        manifest.Checksums["prompts/test.md"] = "def456";

        // Act
        var json = manifest.ToJson();

        // Assert
        json.Should().Contain("\"schemaVersion\": 2");
        json.Should().Contain("\"toolVersion\": \"1.0.0.0\"");
        json.Should().Contain("\"copilot-instructions.md\"");
        json.Should().Contain("\"prompts/test.md\"");
        json.Should().Contain("\"abc123\"");
        json.Should().Contain("\"def456\"");
    }

    [Fact]
    public void FromJson_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """
        {
            "schemaVersion": 2,
            "installedAt": "2026-01-31T12:00:00Z",
            "toolVersion": "1.5.0",
            "source": { "type": "bundled" },
            "assets": ["file1.md", "file2.md"],
            "checksums": {
                "file1.md": "hash1",
                "file2.md": "hash2"
            }
        }
        """;

        // Act
        var manifest = Manifest.FromJson(json);

        // Assert
        manifest.Should().NotBeNull();
        manifest!.SchemaVersion.Should().Be(2);
        manifest.ToolVersion.Should().Be("1.5.0");
        manifest.Assets.Should().HaveCount(2);
        manifest.Assets.Should().Contain("file1.md");
        manifest.Assets.Should().Contain("file2.md");
        manifest.Checksums.Should().HaveCount(2);
        manifest.Checksums["file1.md"].Should().Be("hash1");
        manifest.Checksums["file2.md"].Should().Be("hash2");
    }

    [Fact]
    public void FromJson_WithInvalidJson_ShouldReturnNull()
    {
        // Arrange
        var invalidJson = "not valid json";

        // Act
        var action = () => Manifest.FromJson(invalidJson);

        // Assert
        action.Should().Throw<System.Text.Json.JsonException>();
    }

    [Fact]
    public void RoundTrip_ShouldPreserveData()
    {
        // Arrange
        var original = Manifest.Create("2.0.0", TemplateSource.Remote("test/repo", "develop"));
        original.Assets.Add("test.md");
        original.Checksums["test.md"] = "checksum123";

        // Act
        var json = original.ToJson();
        var restored = Manifest.FromJson(json);

        // Assert
        restored.Should().NotBeNull();
        restored!.SchemaVersion.Should().Be(original.SchemaVersion);
        restored.ToolVersion.Should().Be(original.ToolVersion);
        restored.Source.Type.Should().Be(original.Source.Type);
        restored.Source.Repo.Should().Be(original.Source.Repo);
        restored.Source.Branch.Should().Be(original.Source.Branch);
        restored.Assets.Should().BeEquivalentTo(original.Assets);
        restored.Checksums.Should().BeEquivalentTo(original.Checksums);
    }

    [Fact]
    public void FileName_ShouldBeCorrect()
    {
        // Assert
        Manifest.FileName.Should().Be(".copilot-assets.json");
    }

    [Fact]
    public void RelativePath_ShouldBeCorrect()
    {
        // Assert
        Manifest.RelativePath.Should().Be(".github/.copilot-assets.json");
    }
}
