using DevTools.CopilotAssets.Domain;
using DevTools.CopilotAssets.Services;

namespace DevTools.CopilotAssets.Tests.Services;

public class PolicyAppServiceTests
{
    private readonly Mock<IFileSystemService> _mockFileSystem;
    private readonly Mock<IGitService> _mockGit;
    private readonly PolicyAppService _sut;

    public PolicyAppServiceTests()
    {
        _mockFileSystem = new Mock<IFileSystemService>();
        _mockGit = new Mock<IGitService>();

        // Create real engines with mocked dependencies
        var syncEngine = new SyncEngine(_mockFileSystem.Object, _mockGit.Object);
        var validationEngine = new ValidationEngine(_mockFileSystem.Object, syncEngine);

        _sut = new PolicyAppService(
            _mockFileSystem.Object,
            _mockGit.Object,
            syncEngine,
            validationEngine);
    }

    [Fact]
    public async Task InitAsync_WhenAlreadyInitialized_AndNoForce_ShouldReturnWarning()
    {
        // Arrange
        var manifest = new Manifest
        {
            Version = "1.0.0",
            ToolVersion = "1.0.0",
            InstalledAt = DateTime.UtcNow
        };

        _mockFileSystem.Setup(f => f.GetFullPath(It.IsAny<string>())).Returns("/target");
        _mockFileSystem.Setup(f => f.CombinePath(It.IsAny<string[]>()))
            .Returns<string[]>(paths => string.Join("/", paths));
        _mockFileSystem.Setup(f => f.Exists(It.Is<string>(s => s.Contains(".copilot-assets.json"))))
            .Returns(true);
        _mockFileSystem.Setup(f => f.ReadAllText(It.Is<string>(s => s.Contains(".copilot-assets.json"))))
            .Returns(manifest.ToJson());

        var options = new InitOptions { TargetDirectory = ".", Force = false };

        // Act
        var result = await _sut.InitAsync(options);

        // Assert
        result.Warnings.Should().Contain(w => w.Contains("already installed"));
    }

    [Fact]
    public async Task InitAsync_WhenAlreadyInitialized_WithForce_ShouldReinstall()
    {
        // Arrange
        var manifest = new Manifest
        {
            Version = "1.0.0",
            ToolVersion = "1.0.0",
            InstalledAt = DateTime.UtcNow
        };

        _mockFileSystem.Setup(f => f.GetFullPath(It.IsAny<string>())).Returns("/target");
        _mockFileSystem.Setup(f => f.CombinePath(It.IsAny<string[]>()))
            .Returns<string[]>(paths => string.Join("/", paths));
        _mockFileSystem.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(f => f.ReadAllText(It.Is<string>(s => s.Contains(".copilot-assets.json"))))
            .Returns(manifest.ToJson());
        _mockFileSystem.Setup(f => f.GetFiles(It.IsAny<string>(), "*", true)).Returns([]);
        _mockFileSystem.Setup(f => f.ComputeChecksum(It.IsAny<string>())).Returns("hash");
        _mockGit.Setup(g => g.IsRepository(It.IsAny<string>())).Returns(false);

        var options = new InitOptions { TargetDirectory = ".", Force = true, NoGit = true };

        // Act
        var result = await _sut.InitAsync(options);

        // Assert - should not have the "already installed" warning
        result.Warnings.Should().NotContain(w => w.Contains("already installed"));
    }

    [Fact]
    public async Task InitAsync_WithGitRepo_ShouldUpdateGitignore()
    {
        // Arrange
        _mockFileSystem.Setup(f => f.GetFullPath(It.IsAny<string>())).Returns("/target");
        _mockFileSystem.Setup(f => f.CombinePath(It.IsAny<string[]>()))
            .Returns<string[]>(paths => string.Join("/", paths));
        _mockFileSystem.Setup(f => f.Exists(It.Is<string>(s => s.Contains("templates")))).Returns(true);
        _mockFileSystem.Setup(f => f.Exists(It.Is<string>(s => !s.Contains("templates")))).Returns(false);
        _mockFileSystem.Setup(f => f.GetFiles(It.IsAny<string>(), "*", true)).Returns([]);
        _mockFileSystem.Setup(f => f.ComputeChecksum(It.IsAny<string>())).Returns("hash");
        _mockGit.Setup(g => g.IsRepository(It.IsAny<string>())).Returns(true);

        var options = new InitOptions { TargetDirectory = ".", NoGit = false };

        // Act
        await _sut.InitAsync(options);

        // Assert
        _mockGit.Verify(g => g.EnsureGitignoreAllowsCopilotAssets(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task InitAsync_WithNoGit_ShouldNotUpdateGitignore()
    {
        // Arrange
        _mockFileSystem.Setup(f => f.GetFullPath(It.IsAny<string>())).Returns("/target");
        _mockFileSystem.Setup(f => f.CombinePath(It.IsAny<string[]>()))
            .Returns<string[]>(paths => string.Join("/", paths));
        _mockFileSystem.Setup(f => f.Exists(It.Is<string>(s => s.Contains("templates")))).Returns(true);
        _mockFileSystem.Setup(f => f.Exists(It.Is<string>(s => !s.Contains("templates")))).Returns(false);
        _mockFileSystem.Setup(f => f.GetFiles(It.IsAny<string>(), "*", true)).Returns([]);
        _mockFileSystem.Setup(f => f.ComputeChecksum(It.IsAny<string>())).Returns("hash");

        var options = new InitOptions { TargetDirectory = ".", NoGit = true };

        // Act
        await _sut.InitAsync(options);

        // Assert
        _mockGit.Verify(g => g.EnsureGitignoreAllowsCopilotAssets(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotInitialized_ShouldReturnError()
    {
        // Arrange
        _mockFileSystem.Setup(f => f.GetFullPath(It.IsAny<string>())).Returns("/target");
        _mockFileSystem.Setup(f => f.CombinePath(It.IsAny<string[]>()))
            .Returns<string[]>(paths => string.Join("/", paths));
        _mockFileSystem.Setup(f => f.Exists(It.IsAny<string>())).Returns(false);

        var options = new UpdateOptions { TargetDirectory = "." };

        // Act
        var result = await _sut.UpdateAsync(options);

        // Assert
        result.IsCompliant.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("not installed"));
    }

    [Fact]
    public async Task UpdateAsync_WhenAlreadyLatest_ShouldReturnInfo()
    {
        // Arrange
        var manifest = new Manifest
        {
            Version = SyncEngine.AssetVersion, // Same as current
            ToolVersion = "1.0.0",
            InstalledAt = DateTime.UtcNow
        };

        _mockFileSystem.Setup(f => f.GetFullPath(It.IsAny<string>())).Returns("/target");
        _mockFileSystem.Setup(f => f.CombinePath(It.IsAny<string[]>()))
            .Returns<string[]>(paths => string.Join("/", paths));
        _mockFileSystem.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(f => f.ReadAllText(It.Is<string>(s => s.Contains(".copilot-assets.json"))))
            .Returns(manifest.ToJson());

        var options = new UpdateOptions { TargetDirectory = ".", Force = false };

        // Act
        var result = await _sut.UpdateAsync(options);

        // Assert
        result.Info.Should().Contain(i => i.Contains("already at latest"));
    }

    [Fact]
    public async Task ValidateAsync_ShouldDelegateToValidationEngine()
    {
        // Arrange
        _mockFileSystem.Setup(f => f.GetFullPath(It.IsAny<string>())).Returns("/target");
        _mockFileSystem.Setup(f => f.CombinePath(It.IsAny<string[]>()))
            .Returns<string[]>(paths => string.Join("/", paths));
        _mockFileSystem.Setup(f => f.Exists(It.IsAny<string>())).Returns(false);

        var options = new ValidateOptions { TargetDirectory = ".", CiMode = false };

        // Act
        var result = await _sut.ValidateAsync(options);

        // Assert
        result.Should().NotBeNull();
        result.IsCompliant.Should().BeFalse(); // No .github directory
    }

    [Fact]
    public async Task DiagnoseAsync_ShouldReturnDiagnosticsResult()
    {
        // Arrange
        _mockFileSystem.Setup(f => f.GetFullPath(It.IsAny<string>())).Returns("/current");
        _mockFileSystem.Setup(f => f.CombinePath(It.IsAny<string[]>()))
            .Returns<string[]>(paths => string.Join("/", paths));
        _mockFileSystem.Setup(f => f.Exists(It.IsAny<string>())).Returns(false);
        _mockGit.Setup(g => g.IsGitAvailable()).Returns(true);
        _mockGit.Setup(g => g.IsRepository(It.IsAny<string>())).Returns(true);

        // Act
        var result = await _sut.DiagnoseAsync();

        // Assert
        result.Should().NotBeNull();
        result.GitAvailable.Should().BeTrue();
        result.IsGitRepository.Should().BeTrue();
        result.ToolVersion.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DiagnoseAsync_WhenGitNotAvailable_ShouldReportIssue()
    {
        // Arrange
        _mockFileSystem.Setup(f => f.GetFullPath(It.IsAny<string>())).Returns("/current");
        _mockFileSystem.Setup(f => f.CombinePath(It.IsAny<string[]>()))
            .Returns<string[]>(paths => string.Join("/", paths));
        _mockFileSystem.Setup(f => f.Exists(It.IsAny<string>())).Returns(false);
        _mockGit.Setup(g => g.IsGitAvailable()).Returns(false);
        _mockGit.Setup(g => g.IsRepository(It.IsAny<string>())).Returns(false);

        // Act
        var result = await _sut.DiagnoseAsync();

        // Assert
        result.HasIssues.Should().BeTrue();
        result.Issues.Should().Contain(i => i.Contains("Git"));
    }
}
