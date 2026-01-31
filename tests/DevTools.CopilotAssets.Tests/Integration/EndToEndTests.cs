using DevTools.CopilotAssets.Services;

namespace DevTools.CopilotAssets.Tests.Integration;

/// <summary>
/// Integration tests that verify end-to-end workflows using real file system operations
/// in isolated temporary directories.
/// </summary>
public class EndToEndTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FileSystemService _fileSystem;
    private readonly GitService _gitService;
    private readonly SyncEngine _syncEngine;
    private readonly ValidationEngine _validationEngine;
    private readonly PolicyAppService _appService;

    public EndToEndTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"copilot-assets-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _fileSystem = new FileSystemService();
        _gitService = new GitService(_fileSystem);
        _syncEngine = new SyncEngine(_fileSystem, _gitService);
        _validationEngine = new ValidationEngine(_fileSystem, _syncEngine);
        _appService = new PolicyAppService(_fileSystem, _gitService, _syncEngine, _validationEngine);
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
            // Ignore cleanup errors in tests
        }
    }

    [Fact]
    public async Task FullWorkflow_Init_Update_Validate_ShouldSucceed()
    {
        // Arrange - Create a project directory
        var projectDir = Path.Combine(_tempDir, "my-project");
        Directory.CreateDirectory(projectDir);

        // Act 1 - Initialize
        var initOptions = new InitOptions
        {
            TargetDirectory = projectDir,
            Force = false,
            NoGit = true // Skip git for simplicity in this test
        };
        var initResult = await _appService.InitAsync(initOptions);

        // Assert 1 - Init should succeed
        initResult.IsCompliant.Should().BeTrue(
            because: $"init should succeed. Errors: {string.Join(", ", initResult.Errors)}");

        // Verify files were created
        var githubDir = Path.Combine(projectDir, ".github");
        Directory.Exists(githubDir).Should().BeTrue();

        var manifestPath = Path.Combine(githubDir, ".copilot-assets.json");
        File.Exists(manifestPath).Should().BeTrue();

        // Act 2 - Validate
        var validateOptions = new ValidateOptions
        {
            TargetDirectory = projectDir,
            CiMode = false
        };
        var validateResult = await _appService.ValidateAsync(validateOptions);

        // Assert 2 - Validate should pass
        validateResult.IsCompliant.Should().BeTrue(
            because: $"validate should pass. Errors: {string.Join(", ", validateResult.Errors)}");

        // Act 3 - Update (should report already at latest)
        var updateOptions = new UpdateOptions
        {
            TargetDirectory = projectDir,
            Force = false
        };
        var updateResult = await _appService.UpdateAsync(updateOptions);

        // Assert 3 - Update should succeed with info message
        updateResult.IsCompliant.Should().BeTrue();
        updateResult.Info.Should().Contain(i => i.Contains("latest"));
    }

    [Fact]
    public async Task Init_ThenModifyFile_ShouldWarnOnValidate()
    {
        // Arrange
        var projectDir = Path.Combine(_tempDir, "modified-project");
        Directory.CreateDirectory(projectDir);

        // Init first
        var initOptions = new InitOptions
        {
            TargetDirectory = projectDir,
            NoGit = true
        };
        await _appService.InitAsync(initOptions);

        // Modify a file
        var instructionsPath = Path.Combine(projectDir, ".github", "copilot-instructions.md");
        if (File.Exists(instructionsPath))
        {
            File.AppendAllText(instructionsPath, "\n\n# Custom modifications");
        }

        // Act - Validate (non-strict mode)
        var validateOptions = new ValidateOptions
        {
            TargetDirectory = projectDir,
            CiMode = false
        };
        var result = await _appService.ValidateAsync(validateOptions);

        // Assert - Should have warnings about modifications
        result.IsCompliant.Should().BeTrue(); // Non-strict allows modifications
        result.Warnings.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ForceInit_ShouldOverwriteExistingFiles()
    {
        // Arrange
        var projectDir = Path.Combine(_tempDir, "force-init-project");
        Directory.CreateDirectory(projectDir);

        // Create an existing .github directory with custom content
        var githubDir = Path.Combine(projectDir, ".github");
        Directory.CreateDirectory(githubDir);
        var customFile = Path.Combine(githubDir, "copilot-instructions.md");
        File.WriteAllText(customFile, "# Custom content that should be overwritten");

        // Act - Force init
        var initOptions = new InitOptions
        {
            TargetDirectory = projectDir,
            Force = true,
            NoGit = true
        };
        var result = await _appService.InitAsync(initOptions);

        // Assert
        result.IsCompliant.Should().BeTrue();
        var content = File.ReadAllText(customFile);
        content.Should().NotContain("Custom content that should be overwritten");
    }

    [Fact]
    public async Task Doctor_ShouldReportEnvironmentStatus()
    {
        // Act
        var result = await _appService.DiagnoseAsync();

        // Assert
        result.Should().NotBeNull();
        result.ToolVersion.Should().NotBeNullOrEmpty();
        // Git availability depends on the test environment, but should be a boolean
    }

    [Fact]
    public async Task Validate_WhenNotInitialized_ShouldFail()
    {
        // Arrange - Empty directory
        var projectDir = Path.Combine(_tempDir, "empty-project");
        Directory.CreateDirectory(projectDir);

        // Act
        var validateOptions = new ValidateOptions
        {
            TargetDirectory = projectDir,
            CiMode = false
        };
        var result = await _appService.ValidateAsync(validateOptions);

        // Assert
        result.IsCompliant.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Update_WhenNotInitialized_ShouldFail()
    {
        // Arrange - Empty directory
        var projectDir = Path.Combine(_tempDir, "not-initialized-project");
        Directory.CreateDirectory(projectDir);

        // Act
        var updateOptions = new UpdateOptions
        {
            TargetDirectory = projectDir
        };
        var result = await _appService.UpdateAsync(updateOptions);

        // Assert
        result.IsCompliant.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("not installed") || e.Contains("not initialized"));
    }
}
