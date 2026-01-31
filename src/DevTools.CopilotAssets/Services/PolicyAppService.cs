using DevTools.CopilotAssets.Domain;

namespace DevTools.CopilotAssets.Services;

/// <summary>
/// Main application service orchestrating all operations.
/// </summary>
public sealed class PolicyAppService : IPolicyAppService
{
    private readonly IFileSystemService _fileSystem;
    private readonly IGitService _git;
    private readonly SyncEngine _syncEngine;
    private readonly ValidationEngine _validationEngine;

    public PolicyAppService(
        IFileSystemService fileSystem,
        IGitService git,
        SyncEngine syncEngine,
        ValidationEngine validationEngine)
    {
        _fileSystem = fileSystem;
        _git = git;
        _syncEngine = syncEngine;
        _validationEngine = validationEngine;
    }

    /// <inheritdoc />
    public async Task<ValidationResult> InitAsync(InitOptions options)
    {
        var result = new ValidationResult();
        var targetDir = _fileSystem.GetFullPath(options.TargetDirectory);

        // Check if already initialized
        var existingManifest = _syncEngine.ReadManifest(targetDir);
        if (existingManifest != null && !options.Force)
        {
            result.Warnings.Add($"Assets already installed (version {existingManifest.Version}). Use --force to reinstall.");
            return result;
        }

        // Sync assets
        var syncResult = _syncEngine.SyncAssets(targetDir, options.Force);

        if (!syncResult.Success)
        {
            result.Errors.AddRange(syncResult.Errors);
            return result;
        }

        // Update .gitignore to ensure Copilot assets are tracked
        if (!options.NoGit && _git.IsRepository(targetDir))
        {
            _git.EnsureGitignoreAllowsCopilotAssets(targetDir);
        }

        // Git operations
        if (!options.NoGit && _git.IsRepository(targetDir))
        {
            var filesToStage = syncResult.Synced
                .Select(s => s.FullPath)
                .ToList();

            // Also stage .gitignore if it was modified
            var gitignorePath = _fileSystem.CombinePath(targetDir, ".gitignore");
            if (_fileSystem.Exists(gitignorePath))
            {
                filesToStage.Add(gitignorePath);
            }

            _git.Stage(targetDir, filesToStage.ToArray());
            _git.Commit(targetDir, $"chore: install copilot assets v{SyncEngine.AssetVersion}");

            result.Info.Add("Changes committed to git");
        }

        result.Info.Add($"✓ Installed {syncResult.Synced.Count} asset(s)");
        result.Warnings.AddRange(syncResult.Warnings);

        return await Task.FromResult(result);
    }

    /// <inheritdoc />
    public async Task<ValidationResult> UpdateAsync(UpdateOptions options)
    {
        var result = new ValidationResult();
        var targetDir = _fileSystem.GetFullPath(options.TargetDirectory);

        // Check if initialized
        var existingManifest = _syncEngine.ReadManifest(targetDir);
        if (existingManifest == null)
        {
            result.Errors.Add("Assets not installed. Run 'copilot-assets init' first.");
            return result;
        }

        // Check if update is needed
        if (existingManifest.Version == SyncEngine.AssetVersion && !options.Force)
        {
            result.Info.Add($"Assets are already at latest version ({SyncEngine.AssetVersion})");
            return result;
        }

        // Sync with force to update
        var syncResult = _syncEngine.SyncAssets(targetDir, force: true);

        if (!syncResult.Success)
        {
            result.Errors.AddRange(syncResult.Errors);
            return result;
        }

        // Update .gitignore
        if (!options.NoGit && _git.IsRepository(targetDir))
        {
            _git.EnsureGitignoreAllowsCopilotAssets(targetDir);
        }

        // Git operations
        if (!options.NoGit && _git.IsRepository(targetDir))
        {
            var filesToStage = syncResult.Synced
                .Select(s => s.FullPath)
                .ToList();

            var gitignorePath = _fileSystem.CombinePath(targetDir, ".gitignore");
            if (_fileSystem.Exists(gitignorePath))
            {
                filesToStage.Add(gitignorePath);
            }

            _git.Stage(targetDir, filesToStage.ToArray());
            _git.Commit(targetDir, $"chore: update copilot assets to v{SyncEngine.AssetVersion}");

            result.Info.Add("Changes committed to git");
        }

        var updated = syncResult.Synced.Count(s => s.WasUpdated);
        var added = syncResult.Synced.Count(s => !s.WasUpdated);

        result.Info.Add($"✓ Updated {updated} asset(s), added {added} new asset(s)");
        result.Warnings.AddRange(syncResult.Warnings);

        return await Task.FromResult(result);
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ValidateAsync(ValidateOptions options)
    {
        var targetDir = _fileSystem.GetFullPath(options.TargetDirectory);
        var result = _validationEngine.Validate(targetDir, strictMode: options.CiMode);
        return await Task.FromResult(result);
    }

    /// <inheritdoc />
    public async Task<DiagnosticsResult> DiagnoseAsync()
    {
        var currentDir = _fileSystem.GetFullPath(".");
        var gitHubPath = _fileSystem.CombinePath(currentDir, ".github");
        var manifest = _syncEngine.ReadManifest(currentDir);

        var result = new DiagnosticsResult
        {
            GitAvailable = _git.IsGitAvailable(),
            IsGitRepository = _git.IsRepository("."),
            ToolVersion = SyncEngine.ToolVersion,
            AssetsDirectoryExists = _fileSystem.Exists(gitHubPath),
            ManifestExists = manifest != null,
            InstalledVersion = manifest?.Version
        };

        // Check for issues
        if (!result.GitAvailable)
        {
            result.Issues.Add("Git is not available (LibGit2Sharp not working)");
        }

        if (!result.IsGitRepository)
        {
            result.Issues.Add("Current directory is not a Git repository");
        }

        var templatesPath = _syncEngine.GetTemplatesPath();
        if (!_fileSystem.Exists(templatesPath))
        {
            result.Issues.Add($"Templates directory not found: {templatesPath}");
        }

        return await Task.FromResult(result);
    }
}
