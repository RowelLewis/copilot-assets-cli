using DevTools.CopilotAssets.Domain;
using DevTools.CopilotAssets.Services;

namespace DevTools.CopilotAssets.Tests.Services;

public class SyncEngineTests
{
    [Fact]
    public void ToolVersion_ShouldReturnVersion()
    {
        // Act
        var version = SyncEngine.ToolVersion;

        // Assert
        version.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetTemplatesPath_WithRealFileSystem_ShouldReturnPath()
    {
        // Arrange - Use real file system service
        var fileSystem = new FileSystemService();
        var gitService = new GitService(fileSystem);
        var sut = new SyncEngine(fileSystem, gitService);

        // Act
        var path = sut.GetTemplatesPath();

        // Assert
        path.Should().Contain("templates");
        path.Should().Contain(".github");
    }

    [Fact]
    public void ReadManifest_WhenFileNotExists_ShouldReturnNull()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystemService>();
        var mockGit = new Mock<IGitService>();
        var sut = new SyncEngine(mockFileSystem.Object, mockGit.Object);

        mockFileSystem.Setup(f => f.CombinePath(It.IsAny<string[]>()))
            .Returns<string[]>(paths => string.Join("/", paths));
        mockFileSystem.Setup(f => f.Exists(It.IsAny<string>())).Returns(false);

        // Act
        var result = sut.ReadManifest("/target");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ReadManifest_WhenFileExists_ShouldReturnManifest()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystemService>();
        var mockGit = new Mock<IGitService>();
        var sut = new SyncEngine(mockFileSystem.Object, mockGit.Object);

        var manifestJson = """
        {
            "schemaVersion": 2,
            "installedAt": "2026-01-31T12:00:00Z",
            "toolVersion": "1.0.0.0",
            "source": { "type": "bundled" },
            "assets": [],
            "checksums": {}
        }
        """;

        mockFileSystem.Setup(f => f.CombinePath(It.IsAny<string[]>()))
            .Returns<string[]>(paths => string.Join("/", paths));
        mockFileSystem.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
        mockFileSystem.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(manifestJson);

        // Act
        var result = sut.ReadManifest("/target");

        // Assert
        result.Should().NotBeNull();
        result!.SchemaVersion.Should().Be(2);
    }

    [Fact]
    public void SyncResult_Success_ShouldHaveNoErrors()
    {
        // Arrange
        var result = new SyncResult();

        // Assert
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void SyncResult_WithErrors_ShouldNotBeSuccess()
    {
        // Arrange
        var result = new SyncResult();
        result.Errors.Add("Some error");

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public void SyncResult_CanTrackSyncedAssets()
    {
        // Arrange
        var result = new SyncResult();
        result.Synced.Add(new SyncedAsset
        {
            RelativePath = "file1.md",
            FullPath = "/target/file1.md",
            Checksum = "abc123"
        });

        // Assert
        result.Synced.Should().ContainSingle();
    }

    [Fact]
    public void SyncResult_CanTrackSkippedFiles()
    {
        // Arrange
        var result = new SyncResult();
        result.Skipped.Add("skipped.md");
        result.Warnings.Add("Warning: file skipped");

        // Assert
        result.Skipped.Should().ContainSingle();
        result.Warnings.Should().ContainSingle();
    }
}
