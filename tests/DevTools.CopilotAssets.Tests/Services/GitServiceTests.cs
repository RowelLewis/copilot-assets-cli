using DevTools.CopilotAssets.Services;

namespace DevTools.CopilotAssets.Tests.Services;

public class GitServiceTests
{
    private readonly Mock<IFileSystemService> _mockFileSystem;
    private readonly GitService _sut;

    public GitServiceTests()
    {
        _mockFileSystem = new Mock<IFileSystemService>();
        _sut = new GitService(_mockFileSystem.Object);
    }

    [Fact]
    public void IsGitAvailable_ShouldReturnBoolean()
    {
        // This test checks that the method doesn't throw
        // Git availability depends on the test environment

        // Act
        var result = _sut.IsGitAvailable();

        // Assert - just ensure it returns without throwing
        // The actual value depends on the environment
        (result == true || result == false).Should().BeTrue();
    }

    [Fact]
    public void IsRepository_WhenPathIsNull_ShouldReturnFalse()
    {
        // Act
        var result = _sut.IsRepository(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsRepository_WhenPathIsEmpty_ShouldReturnFalse()
    {
        // Act
        var result = _sut.IsRepository("");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsRepository_WhenPathDoesNotExist_ShouldReturnFalse()
    {
        // Act
        var result = _sut.IsRepository("/nonexistent/path");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetRepositoryRoot_WhenNotARepo_ShouldReturnNull()
    {
        // Arrange - use a temp directory that's not a git repo
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            var result = _sut.GetRepositoryRoot(tempDir);

            // Assert
            result.Should().BeNull();
        }
        finally
        {
            Directory.Delete(tempDir);
        }
    }

    [Fact]
    public void IsClean_WhenNotARepo_ShouldReturnTrue()
    {
        // For non-repos, we consider them "clean" (no uncommitted changes)
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            var result = _sut.IsClean(tempDir);

            // Assert
            result.Should().BeTrue();
        }
        finally
        {
            Directory.Delete(tempDir);
        }
    }

    [Fact]
    public void EnsureGitignoreIgnoresCopilotAssets_WhenNotARepo_ShouldNotThrow()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act & Assert - should not throw
            var act = () => _sut.EnsureGitignoreIgnoresCopilotAssets(tempDir);
            act.Should().NotThrow();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
