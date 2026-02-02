using DevTools.CopilotAssets.Domain.Configuration;

namespace DevTools.CopilotAssets.Tests.Domain.Configuration;

public class RemoteConfigTests
{
    [Fact]
    public void Default_ShouldHaveNullSource()
    {
        // Arrange & Act
        var config = RemoteConfig.Default;

        // Assert
        config.Source.Should().BeNull();
        config.Branch.Should().Be("main");
        config.HasRemoteSource.Should().BeFalse();
    }

    [Theory]
    [InlineData("owner/repo", true)]
    [InlineData("my-org/my-repo", true)]
    [InlineData("test123/test456", true)]
    [InlineData("invalid", false)]
    [InlineData("invalid/repo/extra", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("/repo", false)]
    [InlineData("owner/", false)]
    public void IsValidSource_ShouldValidateFormat(string? source, bool expected)
    {
        // Act
        var result = RemoteConfig.IsValidSource(source!);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void HasRemoteSource_ShouldReturnTrue_WhenSourceIsSet()
    {
        // Arrange
        var config = new RemoteConfig { Source = "owner/repo" };

        // Act & Assert
        config.HasRemoteSource.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void HasRemoteSource_ShouldReturnFalse_WhenSourceIsNullOrEmpty(string? source)
    {
        // Arrange
        var config = new RemoteConfig { Source = source };

        // Act & Assert
        config.HasRemoteSource.Should().BeFalse();
    }

    [Fact]
    public void SaveAndLoad_ShouldPersistConfiguration()
    {
        // Arrange
        var configPath = RemoteConfig.GetConfigPath();
        var configDir = Path.GetDirectoryName(configPath)!;

        // Clean up before test
        if (File.Exists(configPath))
        {
            File.Delete(configPath);
        }

        var originalConfig = new RemoteConfig
        {
            Source = "test-org/test-repo",
            Branch = "develop"
        };

        try
        {
            // Act
            originalConfig.Save();
            var loadedConfig = RemoteConfig.Load();

            // Assert
            loadedConfig.Source.Should().Be("test-org/test-repo");
            loadedConfig.Branch.Should().Be("develop");
            loadedConfig.HasRemoteSource.Should().BeTrue();
        }
        finally
        {
            // Cleanup
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
        }
    }

    [Fact]
    public void Load_ShouldReturnDefault_WhenFileDoesNotExist()
    {
        // Arrange
        var configPath = RemoteConfig.GetConfigPath();
        if (File.Exists(configPath))
        {
            File.Delete(configPath);
        }

        // Act
        var config = RemoteConfig.Load();

        // Assert
        config.Source.Should().BeNull();
        config.Branch.Should().Be("main");
        config.HasRemoteSource.Should().BeFalse();
    }

    [Fact]
    public void Reset_ShouldDeleteConfigFile()
    {
        // Arrange
        var config = new RemoteConfig { Source = "test/repo" };
        config.Save();
        var configPath = RemoteConfig.GetConfigPath();

        // Act
        RemoteConfig.Reset();

        // Assert
        File.Exists(configPath).Should().BeFalse();
    }

    [Fact]
    public void GetConfigPath_ShouldReturnCorrectPath()
    {
        // Act
        var path = RemoteConfig.GetConfigPath();

        // Assert
        path.Should().Contain(".config");
        path.Should().Contain("copilot-assets");
        path.Should().EndWith("config.json");
    }
}
