using DevTools.CopilotAssets.Services;
using DevTools.CopilotAssets.Services.Templates;

namespace DevTools.CopilotAssets.Tests.Services.Templates;

public class BundledTemplateProviderTests
{
    private readonly Mock<IFileSystemService> _mockFileSystem;
    private readonly BundledTemplateProvider _provider;

    public BundledTemplateProviderTests()
    {
        _mockFileSystem = new Mock<IFileSystemService>();
        _provider = new BundledTemplateProvider(_mockFileSystem.Object);
    }

    [Fact]
    public async Task GetTemplatesAsync_ShouldReturnTemplates_WhenDirectoryExists()
    {
        // Arrange
        var templatesPath = Path.Combine(AppContext.BaseDirectory, "templates", ".github");
        var files = new[]
        {
            Path.Combine(templatesPath, "copilot-instructions.md"),
            Path.Combine(templatesPath, "agents", "test.agent.md")
        };

        _mockFileSystem.Setup(fs => fs.Exists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(fs => fs.GetFiles(It.IsAny<string>(), "*", true)).Returns(files);
        _mockFileSystem.Setup(fs => fs.ReadAllText(files[0])).Returns("instruction content");
        _mockFileSystem.Setup(fs => fs.ReadAllText(files[1])).Returns("agent content");

        // Act
        var result = await _provider.GetTemplatesAsync();

        // Assert
        result.HasTemplates.Should().BeTrue();
        result.Templates.Should().HaveCount(2);
        result.Source.Should().Be("bundled");
        result.FromCache.Should().BeFalse();
        result.Templates[0].RelativePath.Should().Be("copilot-instructions.md");
        result.Templates[1].RelativePath.Should().Be("agents/test.agent.md");
    }

    [Fact]
    public async Task GetTemplatesAsync_ShouldReturnError_WhenDirectoryDoesNotExist()
    {
        // Arrange
        _mockFileSystem.Setup(fs => fs.Exists(It.IsAny<string>())).Returns(false);

        // Act
        var result = await _provider.GetTemplatesAsync();

        // Assert
        result.HasError.Should().BeTrue();
        result.HasTemplates.Should().BeFalse();
        result.Error.Should().Contain("Templates directory not found");
    }

    [Fact]
    public async Task IsAvailableAsync_ShouldReturnTrue_WhenDirectoryExists()
    {
        // Arrange
        _mockFileSystem.Setup(fs => fs.Exists(It.IsAny<string>())).Returns(true);

        // Act
        var result = await _provider.IsAvailableAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAvailableAsync_ShouldReturnFalse_WhenDirectoryDoesNotExist()
    {
        // Arrange
        _mockFileSystem.Setup(fs => fs.Exists(It.IsAny<string>())).Returns(false);

        // Act
        var result = await _provider.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }
}
