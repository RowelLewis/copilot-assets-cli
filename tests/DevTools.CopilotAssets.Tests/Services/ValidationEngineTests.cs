using DevTools.CopilotAssets.Domain;
using DevTools.CopilotAssets.Services;

namespace DevTools.CopilotAssets.Tests.Services;

public class ValidationEngineTests
{
    private readonly Mock<IFileSystemService> _mockFileSystem;
    private readonly ValidationEngine _sut;

    public ValidationEngineTests()
    {
        _mockFileSystem = new Mock<IFileSystemService>();
        var mockGit = new Mock<IGitService>();
        var syncEngine = new SyncEngine(_mockFileSystem.Object, mockGit.Object);
        _sut = new ValidationEngine(_mockFileSystem.Object, syncEngine);
    }

    [Fact]
    public void Validate_WhenGitHubDirectoryMissing_ShouldReturnError()
    {
        // Arrange
        _mockFileSystem.Setup(f => f.CombinePath(It.IsAny<string[]>()))
            .Returns<string[]>(paths => string.Join("/", paths));
        _mockFileSystem.Setup(f => f.Exists(It.IsAny<string>())).Returns(false);

        // Act
        var result = _sut.Validate("/target");

        // Assert
        result.IsCompliant.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains(".github") || e.Contains("directory"));
    }

    [Fact]
    public void Validate_WhenManifestMissing_ShouldReturnError()
    {
        // Arrange
        _mockFileSystem.Setup(f => f.CombinePath(It.IsAny<string[]>()))
            .Returns<string[]>(paths => string.Join("/", paths));
        _mockFileSystem.Setup(f => f.Exists(It.Is<string>(s => s.EndsWith(".github"))))
            .Returns(true);
        _mockFileSystem.Setup(f => f.Exists(It.Is<string>(s => s.Contains(".copilot-assets.json"))))
            .Returns(false);
        _mockFileSystem.Setup(f => f.IsDirectory(It.IsAny<string>())).Returns(true);

        // Act
        var result = _sut.Validate("/target");

        // Assert
        result.IsCompliant.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("manifest") || e.Contains("not installed"));
    }

    [Fact]
    public void ValidationResult_WhenNoErrors_ShouldBeCompliant()
    {
        // Arrange & Act
        var result = new ValidationResult();

        // Assert
        result.IsCompliant.Should().BeTrue();
    }

    [Fact]
    public void ValidationResult_WhenHasErrors_ShouldNotBeCompliant()
    {
        // Arrange
        var result = new ValidationResult();
        result.Errors.Add("Some error");

        // Assert
        result.IsCompliant.Should().BeFalse();
    }

    [Fact]
    public void ValidationResult_Success_ShouldCreateEmptyResult()
    {
        // Act
        var result = ValidationResult.Success();

        // Assert
        result.IsCompliant.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidationResult_Failure_ShouldCreateResultWithErrors()
    {
        // Act
        var result = ValidationResult.Failure("Error 1", "Error 2");

        // Assert
        result.IsCompliant.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain("Error 1");
        result.Errors.Should().Contain("Error 2");
    }

    [Fact]
    public void ValidationResult_CanHaveWarningsWithoutErrors()
    {
        // Arrange
        var result = ValidationResult.Success();
        result.Warnings.Add("Some warning");

        // Assert
        result.IsCompliant.Should().BeTrue(); // Warnings don't affect compliance
        result.Warnings.Should().ContainSingle();
    }

    [Fact]
    public void ValidationResult_CanHaveInfoMessages()
    {
        // Arrange
        var result = ValidationResult.Success();
        result.Info.Add("Validation passed");

        // Assert
        result.Info.Should().ContainSingle();
    }
}
