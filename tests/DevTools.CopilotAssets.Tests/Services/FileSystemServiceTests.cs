using DevTools.CopilotAssets.Services;

namespace DevTools.CopilotAssets.Tests.Services;

public class FileSystemServiceTests : IDisposable
{
    private readonly FileSystemService _sut;
    private readonly string _testDirectory;

    public FileSystemServiceTests()
    {
        _sut = new FileSystemService();
        _testDirectory = Path.Combine(Path.GetTempPath(), $"copilot-assets-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Exists_WithExistingFile_ShouldReturnTrue()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePath, "content");

        // Act
        var result = _sut.Exists(filePath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Exists_WithExistingDirectory_ShouldReturnTrue()
    {
        // Arrange
        var dirPath = Path.Combine(_testDirectory, "subdir");
        Directory.CreateDirectory(dirPath);

        // Act
        var result = _sut.Exists(dirPath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Exists_WithNonExistingPath_ShouldReturnFalse()
    {
        // Arrange
        var path = Path.Combine(_testDirectory, "nonexistent");

        // Act
        var result = _sut.Exists(path);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsDirectory_WithDirectory_ShouldReturnTrue()
    {
        // Arrange
        var dirPath = Path.Combine(_testDirectory, "testdir");
        Directory.CreateDirectory(dirPath);

        // Act
        var result = _sut.IsDirectory(dirPath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsDirectory_WithFile_ShouldReturnFalse()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePath, "content");

        // Act
        var result = _sut.IsDirectory(filePath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CreateDirectory_ShouldCreateDirectory()
    {
        // Arrange
        var dirPath = Path.Combine(_testDirectory, "newdir");

        // Act
        _sut.CreateDirectory(dirPath);

        // Assert
        Directory.Exists(dirPath).Should().BeTrue();
    }

    [Fact]
    public void CreateDirectory_WithNestedPath_ShouldCreateAllDirectories()
    {
        // Arrange
        var dirPath = Path.Combine(_testDirectory, "level1", "level2", "level3");

        // Act
        _sut.CreateDirectory(dirPath);

        // Assert
        Directory.Exists(dirPath).Should().BeTrue();
    }

    [Fact]
    public void CopyFile_ShouldCopyFileToTarget()
    {
        // Arrange
        var sourcePath = Path.Combine(_testDirectory, "source.txt");
        var targetPath = Path.Combine(_testDirectory, "target.txt");
        File.WriteAllText(sourcePath, "test content");

        // Act
        _sut.CopyFile(sourcePath, targetPath);

        // Assert
        File.Exists(targetPath).Should().BeTrue();
        File.ReadAllText(targetPath).Should().Be("test content");
    }

    [Fact]
    public void CopyFile_WithOverwrite_ShouldOverwriteExistingFile()
    {
        // Arrange
        var sourcePath = Path.Combine(_testDirectory, "source.txt");
        var targetPath = Path.Combine(_testDirectory, "target.txt");
        File.WriteAllText(sourcePath, "new content");
        File.WriteAllText(targetPath, "old content");

        // Act
        _sut.CopyFile(sourcePath, targetPath, overwrite: true);

        // Assert
        File.ReadAllText(targetPath).Should().Be("new content");
    }

    [Fact]
    public void CopyFile_ShouldCreateTargetDirectory()
    {
        // Arrange
        var sourcePath = Path.Combine(_testDirectory, "source.txt");
        var targetPath = Path.Combine(_testDirectory, "subdir", "target.txt");
        File.WriteAllText(sourcePath, "content");

        // Act
        _sut.CopyFile(sourcePath, targetPath);

        // Assert
        File.Exists(targetPath).Should().BeTrue();
    }

    [Fact]
    public void ReadAllText_ShouldReturnFileContent()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        var content = "Hello, World!\nLine 2";
        File.WriteAllText(filePath, content);

        // Act
        var result = _sut.ReadAllText(filePath);

        // Assert
        result.Should().Be(content);
    }

    [Fact]
    public void WriteAllText_ShouldWriteContent()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "output.txt");
        var content = "Test content\nMultiple lines";

        // Act
        _sut.WriteAllText(filePath, content);

        // Assert
        File.ReadAllText(filePath).Should().Be(content);
    }

    [Fact]
    public void WriteAllText_ShouldCreateDirectory()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "newdir", "output.txt");

        // Act
        _sut.WriteAllText(filePath, "content");

        // Assert
        File.Exists(filePath).Should().BeTrue();
    }

    [Fact]
    public void GetFiles_ShouldReturnFilesInDirectory()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDirectory, "file1.txt"), "");
        File.WriteAllText(Path.Combine(_testDirectory, "file2.txt"), "");
        File.WriteAllText(Path.Combine(_testDirectory, "file3.md"), "");

        // Act
        var result = _sut.GetFiles(_testDirectory);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public void GetFiles_WithPattern_ShouldFilterFiles()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDirectory, "file1.txt"), "");
        File.WriteAllText(Path.Combine(_testDirectory, "file2.txt"), "");
        File.WriteAllText(Path.Combine(_testDirectory, "file3.md"), "");

        // Act
        var result = _sut.GetFiles(_testDirectory, "*.txt");

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public void GetFiles_WithRecursive_ShouldIncludeSubdirectories()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDirectory, "file1.txt"), "");
        var subdir = Path.Combine(_testDirectory, "subdir");
        Directory.CreateDirectory(subdir);
        File.WriteAllText(Path.Combine(subdir, "file2.txt"), "");

        // Act
        var result = _sut.GetFiles(_testDirectory, "*", recursive: true);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public void GetFiles_WithNonExistentDirectory_ShouldReturnEmpty()
    {
        // Act
        var result = _sut.GetFiles(Path.Combine(_testDirectory, "nonexistent"));

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetDirectories_ShouldReturnSubdirectories()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_testDirectory, "dir1"));
        Directory.CreateDirectory(Path.Combine(_testDirectory, "dir2"));

        // Act
        var result = _sut.GetDirectories(_testDirectory);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public void DeleteFile_ShouldRemoveFile()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "todelete.txt");
        File.WriteAllText(filePath, "");

        // Act
        _sut.DeleteFile(filePath);

        // Assert
        File.Exists(filePath).Should().BeFalse();
    }

    [Fact]
    public void DeleteFile_WithNonExistentFile_ShouldNotThrow()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "nonexistent.txt");

        // Act
        var action = () => _sut.DeleteFile(filePath);

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void ComputeChecksum_ShouldReturnConsistentHash()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "hashtest.txt");
        File.WriteAllText(filePath, "test content for hashing");

        // Act
        var hash1 = _sut.ComputeChecksum(filePath);
        var hash2 = _sut.ComputeChecksum(filePath);

        // Assert
        hash1.Should().NotBeNullOrEmpty();
        hash1.Should().Be(hash2);
        hash1.Should().MatchRegex("^[a-f0-9]{64}$"); // SHA256 hex
    }

    [Fact]
    public void ComputeChecksum_DifferentContent_ShouldReturnDifferentHashes()
    {
        // Arrange
        var file1 = Path.Combine(_testDirectory, "file1.txt");
        var file2 = Path.Combine(_testDirectory, "file2.txt");
        File.WriteAllText(file1, "content A");
        File.WriteAllText(file2, "content B");

        // Act
        var hash1 = _sut.ComputeChecksum(file1);
        var hash2 = _sut.ComputeChecksum(file2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void GetFullPath_ShouldReturnAbsolutePath()
    {
        // Act
        var result = _sut.GetFullPath("relative/path");

        // Assert
        Path.IsPathRooted(result).Should().BeTrue();
    }

    [Fact]
    public void CombinePath_ShouldJoinPaths()
    {
        // Act
        var result = _sut.CombinePath("base", "sub", "file.txt");

        // Assert
        result.Should().Contain("base");
        result.Should().Contain("sub");
        result.Should().Contain("file.txt");
    }
}
