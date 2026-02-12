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

    private static Manifest CreateTestManifest() => Manifest.Create("1.0.0");

    [Fact]
    public async Task InitAsync_WhenAlreadyInitialized_AndNoForce_ShouldReturnWarning()
    {
        // Arrange
        var manifest = CreateTestManifest();

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
        var manifest = CreateTestManifest();

        _mockFileSystem.Setup(f => f.GetFullPath(It.IsAny<string>())).Returns("/target");
        _mockFileSystem.Setup(f => f.CombinePath(It.IsAny<string[]>()))
            .Returns<string[]>(paths => string.Join("/", paths));
        _mockFileSystem.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(f => f.ReadAllText(It.Is<string>(s => s.Contains(".copilot-assets.json"))))
            .Returns(manifest.ToJson());
        _mockFileSystem.Setup(f => f.GetFiles(It.IsAny<string>(), "*", true)).Returns([]);
        _mockFileSystem.Setup(f => f.ComputeChecksum(It.IsAny<string>())).Returns("a1b2c3d4e5f60000000000000000000000000000000000000000000000000001");
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
        _mockFileSystem.Setup(f => f.ComputeChecksum(It.IsAny<string>())).Returns("a1b2c3d4e5f60000000000000000000000000000000000000000000000000001");
        _mockGit.Setup(g => g.IsRepository(It.IsAny<string>())).Returns(true);

        var options = new InitOptions { TargetDirectory = ".", NoGit = false };

        // Act
        await _sut.InitAsync(options);

        // Assert
        _mockGit.Verify(g => g.EnsureGitignoreIgnoresCopilotAssets(It.IsAny<string>()), Times.Once);
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
        _mockFileSystem.Setup(f => f.ComputeChecksum(It.IsAny<string>())).Returns("a1b2c3d4e5f60000000000000000000000000000000000000000000000000001");

        var options = new InitOptions { TargetDirectory = ".", NoGit = true };

        // Act
        await _sut.InitAsync(options);

        // Assert
        _mockGit.Verify(g => g.EnsureGitignoreIgnoresCopilotAssets(It.IsAny<string>()), Times.Never);
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

    // Note: UpdateAsync with unchanged templates is tested in EndToEndTests
    // because it requires proper file system interactions that are difficult to mock

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

    [Fact]
    public async Task ListAssetsAsync_WhenNoManifest_ShouldReturnEmpty()
    {
        // Arrange
        _mockFileSystem.Setup(f => f.GetFullPath(It.IsAny<string>())).Returns("/target");
        _mockFileSystem.Setup(f => f.CombinePath(It.IsAny<string[]>()))
            .Returns<string[]>(paths => string.Join("/", paths));
        _mockFileSystem.Setup(f => f.Exists(It.IsAny<string>())).Returns(false);

        var options = new ListOptions { TargetDirectory = "." };

        // Act
        var result = await _sut.ListAssetsAsync(options);

        // Assert
        result.Assets.Should().BeEmpty();
        result.Summary.Total.Should().Be(0);
    }

    [Fact]
    public async Task ListAssetsAsync_WithManifest_ShouldReturnAssets()
    {
        // Arrange
        var manifest = CreateTestManifest();
        manifest.Assets.Add("prompts/test.md");
        manifest.Assets.Add(".copilot-assets.json");
        manifest.Checksums["prompts/test.md"] = "a1b2c3d4e5f60000000000000000000000000000000000000000000000000001";

        _mockFileSystem.Setup(f => f.GetFullPath(It.IsAny<string>())).Returns("/target");
        _mockFileSystem.Setup(f => f.CombinePath(It.IsAny<string[]>()))
            .Returns<string[]>(paths => string.Join("/", paths));
        _mockFileSystem.Setup(f => f.Exists(It.Is<string>(s => s.Contains(".copilot-assets.json"))))
            .Returns(true);
        _mockFileSystem.Setup(f => f.Exists(It.Is<string>(s => s.Contains("prompts/test.md"))))
            .Returns(true);
        _mockFileSystem.Setup(f => f.ReadAllText(It.Is<string>(s => s.Contains(".copilot-assets.json"))))
            .Returns(manifest.ToJson());
        _mockFileSystem.Setup(f => f.ComputeChecksum(It.Is<string>(s => s.Contains("test.md"))))
            .Returns("a1b2c3d4e5f60000000000000000000000000000000000000000000000000001");

        var options = new ListOptions { TargetDirectory = "." };

        // Act
        var result = await _sut.ListAssetsAsync(options);

        // Assert
        result.Assets.Should().HaveCount(1);
        result.Assets[0].Name.Should().Be("test.md");
        result.Assets[0].Type.Should().Be("prompts");
        result.Assets[0].Valid.Should().BeTrue();
    }

    [Fact]
    public async Task ListAssetsAsync_WithFilter_ShouldFilterResults()
    {
        // Arrange
        var manifest = CreateTestManifest();
        manifest.Assets.Add("prompts/test.md");
        manifest.Assets.Add("agents/agent.md");
        manifest.Assets.Add(".copilot-assets.json");
        manifest.Checksums["prompts/test.md"] = "a1b2c3d4e5f60000000000000000000000000000000000000000000000000001";
        manifest.Checksums["agents/agent.md"] = "b2c3d4e5f6a10000000000000000000000000000000000000000000000000002";

        _mockFileSystem.Setup(f => f.GetFullPath(It.IsAny<string>())).Returns("/target");
        _mockFileSystem.Setup(f => f.CombinePath(It.IsAny<string[]>()))
            .Returns<string[]>(paths => string.Join("/", paths));
        _mockFileSystem.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(f => f.ReadAllText(It.Is<string>(s => s.Contains(".copilot-assets.json"))))
            .Returns(manifest.ToJson());
        _mockFileSystem.Setup(f => f.ComputeChecksum(It.IsAny<string>())).Returns("a1b2c3d4e5f60000000000000000000000000000000000000000000000000001");

        var filter = AssetTypeFilter.ParseOnly("prompts").Filter;
        var options = new ListOptions { TargetDirectory = ".", Filter = filter };

        // Act
        var result = await _sut.ListAssetsAsync(options);

        // Assert
        result.Assets.Should().HaveCount(1);
        result.Assets[0].Type.Should().Be("prompts");
    }

    [Fact]
    public async Task VerifyAsync_WhenNoManifest_ShouldReturnError()
    {
        // Arrange
        _mockFileSystem.Setup(f => f.GetFullPath(It.IsAny<string>())).Returns("/target");
        _mockFileSystem.Setup(f => f.CombinePath(It.IsAny<string[]>()))
            .Returns<string[]>(paths => string.Join("/", paths));
        _mockFileSystem.Setup(f => f.Exists(It.IsAny<string>())).Returns(false);

        var options = new VerifyOptions { TargetDirectory = "." };

        // Act
        var result = await _sut.VerifyAsync(options);

        // Assert
        result.ExitCode.Should().Be(1);
        result.Errors.Should().Contain(e => e.Contains("manifest"));
    }

    [Fact]
    public async Task VerifyAsync_WithValidAssets_ShouldReturnZeroExitCode()
    {
        // Arrange
        var manifest = CreateTestManifest();
        manifest.Assets.Add("prompts/test.md");
        manifest.Assets.Add(".copilot-assets.json");
        manifest.Checksums["prompts/test.md"] = "a1b2c3d4e5f60000000000000000000000000000000000000000000000000001";

        _mockFileSystem.Setup(f => f.GetFullPath(It.IsAny<string>())).Returns("/target");
        _mockFileSystem.Setup(f => f.CombinePath(It.IsAny<string[]>()))
            .Returns<string[]>(paths => string.Join("/", paths));
        _mockFileSystem.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(f => f.ReadAllText(It.Is<string>(s => s.Contains(".copilot-assets.json"))))
            .Returns(manifest.ToJson());
        _mockFileSystem.Setup(f => f.ComputeChecksum(It.Is<string>(s => s.Contains("test.md"))))
            .Returns("a1b2c3d4e5f60000000000000000000000000000000000000000000000000001");

        var options = new VerifyOptions { TargetDirectory = "." };

        // Act
        var result = await _sut.VerifyAsync(options);

        // Assert
        result.ExitCode.Should().Be(0);
        result.Assets.Should().HaveCount(1);
        result.Assets[0].Status.Should().Be(VerifyStatus.Valid);
    }

    [Fact]
    public async Task VerifyAsync_WithModifiedAsset_ShouldReturnOneExitCode()
    {
        // Arrange
        var manifest = CreateTestManifest();
        manifest.Assets.Add("prompts/test.md");
        manifest.Assets.Add(".copilot-assets.json");
        manifest.Checksums["prompts/test.md"] = "c3d4e5f6a1b20000000000000000000000000000000000000000000000000003";

        _mockFileSystem.Setup(f => f.GetFullPath(It.IsAny<string>())).Returns("/target");
        _mockFileSystem.Setup(f => f.CombinePath(It.IsAny<string[]>()))
            .Returns<string[]>(paths => string.Join("/", paths));
        _mockFileSystem.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(f => f.ReadAllText(It.Is<string>(s => s.Contains(".copilot-assets.json"))))
            .Returns(manifest.ToJson());
        _mockFileSystem.Setup(f => f.ComputeChecksum(It.Is<string>(s => s.Contains("test.md"))))
            .Returns("d4e5f6a1b2c30000000000000000000000000000000000000000000000000004"); // Different checksum

        var options = new VerifyOptions { TargetDirectory = "." };

        // Act
        var result = await _sut.VerifyAsync(options);

        // Assert
        result.ExitCode.Should().Be(1);
        result.Assets.Should().HaveCount(1);
        result.Assets[0].Status.Should().Be(VerifyStatus.Modified);
    }

    [Fact]
    public async Task PreviewInitAsync_WhenNotInitialized_ShouldShowCreates()
    {
        // Arrange
        _mockFileSystem.Setup(f => f.GetFullPath(It.IsAny<string>())).Returns("/target");
        _mockFileSystem.Setup(f => f.CombinePath(It.IsAny<string[]>()))
            .Returns<string[]>(paths => string.Join("/", paths));
        _mockFileSystem.Setup(f => f.Exists(It.Is<string>(s => s.Contains("templates")))).Returns(true);
        _mockFileSystem.Setup(f => f.Exists(It.Is<string>(s => !s.Contains("templates")))).Returns(false);
        _mockFileSystem.Setup(f => f.GetFiles(It.IsAny<string>(), "*", true))
            .Returns(new[] { "/templates/prompts/test.md" });

        var options = new InitOptions { TargetDirectory = "." };

        // Act
        var result = await _sut.PreviewInitAsync(options);

        // Assert
        result.Summary.Creates.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PreviewInitAsync_WhenAlreadyInitialized_WithoutForce_ShouldShowSkip()
    {
        // Arrange
        var manifest = CreateTestManifest();

        _mockFileSystem.Setup(f => f.GetFullPath(It.IsAny<string>())).Returns("/target");
        _mockFileSystem.Setup(f => f.CombinePath(It.IsAny<string[]>()))
            .Returns<string[]>(paths => string.Join("/", paths));
        _mockFileSystem.Setup(f => f.Exists(It.Is<string>(s => s.Contains(".copilot-assets.json"))))
            .Returns(true);
        _mockFileSystem.Setup(f => f.ReadAllText(It.Is<string>(s => s.Contains(".copilot-assets.json"))))
            .Returns(manifest.ToJson());

        var options = new InitOptions { TargetDirectory = ".", Force = false };

        // Act
        var result = await _sut.PreviewInitAsync(options);

        // Assert
        result.Summary.Skips.Should().BeGreaterThan(0);
        result.Summary.Creates.Should().Be(0);
    }
}
