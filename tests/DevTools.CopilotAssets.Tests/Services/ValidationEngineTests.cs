using DevTools.CopilotAssets.Domain;
using DevTools.CopilotAssets.Services;
using DevTools.CopilotAssets.Infrastructure.Security;

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

    [Fact]
    public void Validate_MultiTargetManifest_ResolvesPathsCorrectly()
    {
        // Arrange - simulate a multi-target manifest with claude: and cursor: tracking paths
        var manifest = new Manifest
        {
            InstalledAt = DateTime.UtcNow,
            ToolVersion = "1.0.0",
            Assets = ["copilot-instructions.md", "claude:CLAUDE.md", "cursor:.cursor/rules/instructions.mdc"],
            Checksums = new Dictionary<string, string>
            {
                ["copilot-instructions.md"] = "a1b2c3d4e5f6789012345678901234567890123456789012345678901234abcd",
                ["claude:CLAUDE.md"] = "b2c3d4e5f6789012345678901234567890123456789012345678901234abcde1",
                ["cursor:.cursor/rules/instructions.mdc"] = "c3d4e5f6789012345678901234567890123456789012345678901234abcde12a"
            }
        };

        _mockFileSystem.Setup(f => f.CombinePath(It.IsAny<string[]>()))
            .Returns<string[]>(paths => string.Join("/", paths));

        // .github directory exists
        _mockFileSystem.Setup(f => f.Exists(It.Is<string>(s => s.EndsWith(".github"))))
            .Returns(true);
        // manifest file exists
        _mockFileSystem.Setup(f => f.Exists(It.Is<string>(s => s.Contains(".copilot-assets.json"))))
            .Returns(true);
        _mockFileSystem.Setup(f => f.IsDirectory(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(f => f.ReadAllText(It.Is<string>(s => s.Contains(".copilot-assets.json"))))
            .Returns(System.Text.Json.JsonSerializer.Serialize(manifest));

        // All resolved paths exist
        _mockFileSystem.Setup(f => f.Exists(It.Is<string>(s => s.Contains(".github/copilot-instructions.md"))))
            .Returns(true);
        _mockFileSystem.Setup(f => f.Exists(It.Is<string>(s => s.EndsWith("CLAUDE.md") && !s.Contains(".github"))))
            .Returns(true);
        _mockFileSystem.Setup(f => f.Exists(It.Is<string>(s => s.Contains(".cursor/rules/instructions.mdc"))))
            .Returns(true);
        _mockFileSystem.Setup(f => f.GetFiles(It.IsAny<string>(), "SKILL.md", true))
            .Returns([]);
        _mockFileSystem.Setup(f => f.GetFiles(It.IsAny<string>(), "*", true))
            .Returns([]);

        // All checksums match
        _mockFileSystem.Setup(f => f.ComputeChecksum(It.Is<string>(s => s.Contains(".github/copilot-instructions.md"))))
            .Returns("a1b2c3d4e5f6789012345678901234567890123456789012345678901234abcd");
        _mockFileSystem.Setup(f => f.ComputeChecksum(It.Is<string>(s => s.EndsWith("CLAUDE.md"))))
            .Returns("b2c3d4e5f6789012345678901234567890123456789012345678901234abcde1");
        _mockFileSystem.Setup(f => f.ComputeChecksum(It.Is<string>(s => s.Contains(".cursor/rules/instructions.mdc"))))
            .Returns("c3d4e5f6789012345678901234567890123456789012345678901234abcde12a");

        // Act
        var result = _sut.Validate("/target");

        // Assert - should be compliant with no errors about multi-target paths
        result.Errors.Should().NotContain(e => e.Contains("claude:") || e.Contains("cursor:"),
            "multi-target tracking paths should not appear as-is in error messages");
        result.Errors.Should().NotContain(e => e.Contains(".github/claude:") || e.Contains(".github/cursor:"),
            "multi-target paths should never be prefixed with .github/");
    }

    [Fact]
    public void Validate_MultiTargetMissingAsset_ReportsCorrectPath()
    {
        // Arrange - a missing claude:CLAUDE.md should be reported as "CLAUDE.md", not ".github/claude:CLAUDE.md"
        var manifest = new Manifest
        {
            InstalledAt = DateTime.UtcNow,
            ToolVersion = "1.0.0",
            Assets = ["claude:CLAUDE.md"],
            Checksums = new Dictionary<string, string>
            {
                ["claude:CLAUDE.md"] = "a1b2c3d4e5f6789012345678901234567890123456789012345678901234abcd"
            }
        };

        _mockFileSystem.Setup(f => f.CombinePath(It.IsAny<string[]>()))
            .Returns<string[]>(paths => string.Join("/", paths));
        _mockFileSystem.Setup(f => f.Exists(It.Is<string>(s => s.EndsWith(".github"))))
            .Returns(true);
        _mockFileSystem.Setup(f => f.Exists(It.Is<string>(s => s.Contains(".copilot-assets.json"))))
            .Returns(true);
        _mockFileSystem.Setup(f => f.IsDirectory(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(f => f.ReadAllText(It.Is<string>(s => s.Contains(".copilot-assets.json"))))
            .Returns(System.Text.Json.JsonSerializer.Serialize(manifest));

        // The required file "copilot-instructions.md" also has to exist
        _mockFileSystem.Setup(f => f.Exists(It.Is<string>(s => s.Contains("copilot-instructions.md"))))
            .Returns(true);
        // CLAUDE.md does NOT exist
        _mockFileSystem.Setup(f => f.Exists(It.Is<string>(s => s.EndsWith("CLAUDE.md"))))
            .Returns(false);
        _mockFileSystem.Setup(f => f.GetFiles(It.IsAny<string>(), "SKILL.md", true))
            .Returns([]);
        _mockFileSystem.Setup(f => f.GetFiles(It.IsAny<string>(), "*", true))
            .Returns([]);

        // Act
        var result = _sut.Validate("/target");

        // Assert - error should reference "CLAUDE.md" not ".github/claude:CLAUDE.md"
        result.Errors.Should().Contain(e => e.Contains("CLAUDE.md"),
            "missing multi-target asset should be reported with its resolved path");
        result.Errors.Should().NotContain(e => e.Contains(".github/claude:"),
            "multi-target paths should not be prefixed with .github/");
    }
}
