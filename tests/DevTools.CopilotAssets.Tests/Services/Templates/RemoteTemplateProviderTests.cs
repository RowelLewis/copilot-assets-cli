using DevTools.CopilotAssets.Domain.Configuration;
using DevTools.CopilotAssets.Services;
using DevTools.CopilotAssets.Services.Http;
using DevTools.CopilotAssets.Services.Templates;

namespace DevTools.CopilotAssets.Tests.Services.Templates;

public class RemoteTemplateProviderTests
{
    private readonly Mock<IFileSystemService> _mockFileSystem;
    private readonly RemoteConfig _config;

    public RemoteTemplateProviderTests()
    {
        _mockFileSystem = new Mock<IFileSystemService>();
        _config = new RemoteConfig
        {
            Source = "test-org/test-repo",
            Branch = "main"
        };
    }

    [Fact]
    public async Task GetTemplatesAsync_ShouldReturnBundled_WhenNoRemoteConfigured()
    {
        // Arrange
        var noRemoteConfig = new RemoteConfig { Source = null };
        var templatesPath = Path.Combine(AppContext.BaseDirectory, "templates", ".github");

        _mockFileSystem.Setup(fs => fs.Exists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(fs => fs.GetFiles(It.IsAny<string>(), "*", true))
            .Returns(new[] { Path.Combine(templatesPath, "test.md") });
        _mockFileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>())).Returns("content");

        // Use a simple HttpClient that will fail - it should fall back to default
        var provider = new RemoteTemplateProvider(noRemoteConfig, _mockFileSystem.Object);

        // Act
        var result = await provider.GetTemplatesAsync();

        // Assert
        result.Source.Should().Be("default");
    }

    [Fact]
    public async Task IsAvailableAsync_ShouldReturnFalse_WhenNoRemoteConfigured()
    {
        // Arrange
        var noRemoteConfig = new RemoteConfig { Source = null };
        var provider = new RemoteTemplateProvider(noRemoteConfig, _mockFileSystem.Object);

        // Act
        var result = await provider.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetTemplatesAsync_ShouldUseBranch_WhenConfigured()
    {
        // Arrange
        var config = new RemoteConfig { Source = "test/repo", Branch = "develop" };
        var templatesPath = Path.Combine(AppContext.BaseDirectory, "templates", ".github");

        _mockFileSystem.Setup(fs => fs.Exists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(fs => fs.GetFiles(It.IsAny<string>(), "*", true))
            .Returns(new[] { Path.Combine(templatesPath, "test.md") });
        _mockFileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>())).Returns("content");

        var provider = new RemoteTemplateProvider(config, _mockFileSystem.Object);

        // Act
        var result = await provider.GetTemplatesAsync();

        // Assert - should include the branch in the source
        result.Should().NotBeNull();
        result.Source.Should().Contain("test/repo");
        result.Source.Should().Contain("develop");
    }

    [Fact]
    public async Task GetTemplatesAsync_ShouldHandleEmptySource()
    {
        // Arrange
        var config = new RemoteConfig { Source = "" };
        var templatesPath = Path.Combine(AppContext.BaseDirectory, "templates", ".github");

        _mockFileSystem.Setup(fs => fs.Exists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(fs => fs.GetFiles(It.IsAny<string>(), "*", true))
            .Returns(new[] { Path.Combine(templatesPath, "test.md") });
        _mockFileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>())).Returns("content");

        var provider = new RemoteTemplateProvider(config, _mockFileSystem.Object);

        // Act
        var result = await provider.GetTemplatesAsync();

        // Assert
        result.Source.Should().Be("default");
    }

    [Fact]
    public void Constructor_ShouldAcceptValidConfig()
    {
        // Arrange & Act
        var provider = new RemoteTemplateProvider(_config, _mockFileSystem.Object);

        // Assert
        provider.Should().NotBeNull();
    }
}
