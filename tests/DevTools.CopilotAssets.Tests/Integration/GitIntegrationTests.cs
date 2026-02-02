using DevTools.CopilotAssets.Services;
using LibGit2Sharp;

namespace DevTools.CopilotAssets.Tests.Integration;

/// <summary>
/// Integration tests for Git operations using real repositories.
/// These tests create actual git repositories in temp directories.
/// </summary>
public class GitIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FileSystemService _fileSystem;
    private readonly GitService _gitService;

    public GitIntegrationTests()
    {
        // Use Path.GetFullPath to resolve any symlinks (macOS /var -> /private/var)
        _tempDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"git-test-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(_tempDir);

        _fileSystem = new FileSystemService();
        _gitService = new GitService(_fileSystem);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                // Force delete including read-only files (git objects)
                foreach (var file in Directory.GetFiles(_tempDir, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public void IsRepository_WhenGitRepo_ShouldReturnTrue()
    {
        // Arrange - Initialize a git repository
        Repository.Init(_tempDir);

        // Act
        var result = _gitService.IsRepository(_tempDir);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsRepository_WhenNotGitRepo_ShouldReturnFalse()
    {
        // Act
        var result = _gitService.IsRepository(_tempDir);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetRepositoryRoot_ShouldReturnRootPath()
    {
        // Arrange
        Repository.Init(_tempDir);
        var subDir = Path.Combine(_tempDir, "src", "app");
        Directory.CreateDirectory(subDir);

        // Act
        var root = _gitService.GetRepositoryRoot(subDir);

        // Assert
        root.Should().NotBeNull();
        // Compare the directory names to handle macOS /var -> /private/var symlinks
        var expectedDir = Path.GetFileName(_tempDir.TrimEnd(Path.DirectorySeparatorChar));
        var actualDir = Path.GetFileName(root!.TrimEnd(Path.DirectorySeparatorChar));
        actualDir.Should().Be(expectedDir);
    }

    [Fact]
    public void EnsureGitignoreIgnoresCopilotAssets_WithNoGitignore_ShouldCreateOne()
    {
        // Arrange
        Repository.Init(_tempDir);
        var gitignorePath = Path.Combine(_tempDir, ".gitignore");
        File.Exists(gitignorePath).Should().BeFalse();

        // Act
        _gitService.EnsureGitignoreIgnoresCopilotAssets(_tempDir);

        // Assert
        File.Exists(gitignorePath).Should().BeTrue();
        var content = File.ReadAllText(gitignorePath);
        content.Should().Contain(".github/");
    }

    [Fact]
    public void EnsureGitignoreIgnoresCopilotAssets_WithExistingGitignore_ShouldAppend()
    {
        // Arrange
        Repository.Init(_tempDir);
        var gitignorePath = Path.Combine(_tempDir, ".gitignore");
        File.WriteAllText(gitignorePath, "# Existing content\nnode_modules/\n");

        // Act
        _gitService.EnsureGitignoreIgnoresCopilotAssets(_tempDir);

        // Assert
        var content = File.ReadAllText(gitignorePath);
        content.Should().Contain("node_modules/"); // Original content preserved
        content.Should().Contain(".github/"); // New pattern added
    }
}
