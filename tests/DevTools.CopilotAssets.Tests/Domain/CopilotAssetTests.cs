using DevTools.CopilotAssets.Domain;

namespace DevTools.CopilotAssets.Tests.Domain;

public class CopilotAssetTests
{
    [Fact]
    public void CopilotAsset_ShouldCreateWithRequiredProperties()
    {
        // Arrange & Act
        var asset = new CopilotAsset
        {
            Name = "test-prompt.md",
            SourcePath = "/templates/prompts/test-prompt.md",
            TargetPath = ".github/prompts/test-prompt.md",
            Type = AssetType.Prompt
        };

        // Assert
        asset.Name.Should().Be("test-prompt.md");
        asset.SourcePath.Should().Be("/templates/prompts/test-prompt.md");
        asset.TargetPath.Should().Be(".github/prompts/test-prompt.md");
        asset.Type.Should().Be(AssetType.Prompt);
        asset.Checksum.Should().BeNull();
    }

    [Fact]
    public void CopilotAsset_ShouldSupportOptionalChecksum()
    {
        // Arrange & Act
        var asset = new CopilotAsset
        {
            Name = "test.md",
            SourcePath = "/src",
            TargetPath = "/target",
            Type = AssetType.Instruction,
            Checksum = "abc123def456"
        };

        // Assert
        asset.Checksum.Should().Be("abc123def456");
    }

    [Theory]
    [InlineData(AssetType.Instruction)]
    [InlineData(AssetType.Prompt)]
    [InlineData(AssetType.Agent)]
    [InlineData(AssetType.Skill)]
    public void AssetType_ShouldHaveAllExpectedValues(AssetType type)
    {
        // Assert
        Enum.IsDefined(typeof(AssetType), type).Should().BeTrue();
    }

    [Fact]
    public void CopilotAsset_RecordEquality_ShouldWorkCorrectly()
    {
        // Arrange
        var asset1 = new CopilotAsset
        {
            Name = "test.md",
            SourcePath = "/src",
            TargetPath = "/target",
            Type = AssetType.Prompt
        };

        var asset2 = new CopilotAsset
        {
            Name = "test.md",
            SourcePath = "/src",
            TargetPath = "/target",
            Type = AssetType.Prompt
        };

        var asset3 = new CopilotAsset
        {
            Name = "different.md",
            SourcePath = "/src",
            TargetPath = "/target",
            Type = AssetType.Prompt
        };

        // Assert
        asset1.Should().Be(asset2);
        asset1.Should().NotBe(asset3);
    }
}
