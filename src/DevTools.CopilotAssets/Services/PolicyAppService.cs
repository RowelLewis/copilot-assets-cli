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
            result.Warnings.Add("Assets already installed. Use --force to reinstall.");
            return result;
        }

        // Determine source override
        string? sourceOverride = options.UseDefaultTemplates ? "default" : options.SourceOverride;

        // Sync assets with optional filter and source
        var syncResult = await _syncEngine.SyncAssetsAsync(
            targetDir,
            options.Force,
            options.Filter,
            sourceOverride);

        if (!syncResult.Success)
        {
            result.Errors.AddRange(syncResult.Errors);
            return result;
        }

        // Update .gitignore to ensure Copilot assets are ignored
        if (!options.NoGit && _git.IsRepository(targetDir))
        {
            _git.EnsureGitignoreIgnoresCopilotAssets(targetDir);
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

            // Prompt user for confirmation
            if (PromptUserForCommit(syncResult.Synced.Count))
            {
                _git.Stage(targetDir, filesToStage.ToArray());
                _git.Commit(targetDir, "chore: install copilot assets");
                result.Info.Add("Changes committed to git");
            }
            else
            {
                result.Info.Add("Changes not committed. Files are ready to be staged.");
            }
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

        // Determine source override
        string? sourceOverride = options.UseDefaultTemplates ? "default" : options.SourceOverride;

        // Check for updates using checksums (not fake version numbers)
        if (!options.Force)
        {
            var updateCheck = await _syncEngine.CheckForUpdatesAsync(targetDir, options.Filter, sourceOverride);

            if (!updateCheck.HasChanges)
            {
                var totalFiles = updateCheck.Unchanged.Count;
                result.Info.Add($"✓ No changes detected (all {totalFiles} files match)");
                return result;
            }

            // Show what would change
            result.Info.Add("Changes available:");
            foreach (var file in updateCheck.Modified)
                result.Info.Add($"  Modified: {file}");
            foreach (var file in updateCheck.Added)
                result.Info.Add($"  Added: {file}");
            foreach (var file in updateCheck.Removed)
                result.Info.Add($"  Removed: {file}");
        }

        // Sync with force to update
        var syncResult = await _syncEngine.SyncAssetsAsync(targetDir, force: true, options.Filter, sourceOverride);

        if (!syncResult.Success)
        {
            result.Errors.AddRange(syncResult.Errors);
            return result;
        }

        // Update .gitignore
        if (!options.NoGit && _git.IsRepository(targetDir))
        {
            _git.EnsureGitignoreIgnoresCopilotAssets(targetDir);
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

            // Prompt user for confirmation
            if (PromptUserForCommit(syncResult.Synced.Count))
            {
                _git.Stage(targetDir, filesToStage.ToArray());
                _git.Commit(targetDir, "chore: update copilot assets");
                result.Info.Add("Changes committed to git");
            }
            else
            {
                result.Info.Add("Changes not committed. Files are ready to be staged.");
            }
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
            Source = manifest?.Source
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

    /// <inheritdoc />
    public async Task<AssetListResult> ListAssetsAsync(ListOptions options)
    {
        var targetDir = _fileSystem.GetFullPath(options.TargetDirectory);
        var manifest = _syncEngine.ReadManifest(targetDir);

        if (manifest == null)
        {
            return new AssetListResult(
                ProjectPath: targetDir,
                Assets: [],
                Summary: new AssetSummary(0, 0, 0, 0),
                Source: null);
        }

        var assets = new List<AssetInfo>();
        int valid = 0, modified = 0, missing = 0;

        foreach (var assetPath in manifest.Assets)
        {
            // Skip manifest file itself
            if (assetPath == Manifest.FileName)
                continue;

            var category = AssetTypeFilter.GetCategory(assetPath);

            // Apply filter if specified
            if (options.Filter != null && !options.Filter.ShouldInclude(category))
                continue;

            var fullPath = _fileSystem.CombinePath(targetDir, ".github", assetPath);
            var name = Path.GetFileName(assetPath);
            var type = category.ToString().ToLowerInvariant();

            if (!_fileSystem.Exists(fullPath))
            {
                assets.Add(new AssetInfo(
                    Type: type,
                    Name: name,
                    Path: assetPath,
                    Valid: false,
                    Reason: "missing"));
                missing++;
                continue;
            }

            var currentChecksum = _fileSystem.ComputeChecksum(fullPath);
            var expectedChecksum = manifest.Checksums.GetValueOrDefault(assetPath);
            var isValid = currentChecksum == expectedChecksum;

            assets.Add(new AssetInfo(
                Type: type,
                Name: name,
                Path: assetPath,
                Valid: isValid,
                Checksum: currentChecksum,
                Reason: isValid ? null : "modified"));

            if (isValid) valid++;
            else modified++;
        }

        return await Task.FromResult(new AssetListResult(
            ProjectPath: targetDir,
            Assets: assets,
            Summary: new AssetSummary(assets.Count, valid, modified, missing),
            Source: manifest.Source));
    }

    /// <inheritdoc />
    public async Task<VerifyResult> VerifyAsync(VerifyOptions options)
    {
        var targetDir = _fileSystem.GetFullPath(options.TargetDirectory);
        var manifest = _syncEngine.ReadManifest(targetDir);

        if (manifest == null)
        {
            return await Task.FromResult(VerifyResult.NoManifest());
        }

        var results = new List<VerifyAssetResult>();
        var warnings = new List<string>();

        foreach (var assetPath in manifest.Assets)
        {
            // Skip manifest file itself
            if (assetPath == Manifest.FileName)
                continue;

            var category = AssetTypeFilter.GetCategory(assetPath);

            // Apply filter
            if (options.Filter != null && !options.Filter.ShouldInclude(category))
                continue;

            var fullPath = _fileSystem.CombinePath(targetDir, ".github", assetPath);
            var name = Path.GetFileName(assetPath);
            var type = category.ToString().ToLowerInvariant();
            var expectedChecksum = manifest.Checksums.GetValueOrDefault(assetPath);

            if (!_fileSystem.Exists(fullPath))
            {
                if (options.Restore)
                {
                    // Restore missing file
                    await RestoreAssetAsync(assetPath, fullPath);
                    results.Add(new VerifyAssetResult(
                        Type: type, Name: name, Path: assetPath,
                        Status: VerifyStatus.Restored,
                        ExpectedChecksum: expectedChecksum,
                        ActualChecksum: expectedChecksum));
                }
                else
                {
                    results.Add(new VerifyAssetResult(
                        Type: type, Name: name, Path: assetPath,
                        Status: VerifyStatus.Missing,
                        ExpectedChecksum: expectedChecksum,
                        ActualChecksum: null));
                }
                continue;
            }

            var actualChecksum = _fileSystem.ComputeChecksum(fullPath);
            var isValid = actualChecksum == expectedChecksum;

            if (isValid)
            {
                results.Add(new VerifyAssetResult(
                    Type: type, Name: name, Path: assetPath,
                    Status: VerifyStatus.Valid,
                    ExpectedChecksum: expectedChecksum,
                    ActualChecksum: actualChecksum));
            }
            else if (options.Restore)
            {
                // Restore modified file
                await RestoreAssetAsync(assetPath, fullPath);
                results.Add(new VerifyAssetResult(
                    Type: type, Name: name, Path: assetPath,
                    Status: VerifyStatus.Restored,
                    ExpectedChecksum: expectedChecksum,
                    ActualChecksum: expectedChecksum));
            }
            else
            {
                results.Add(new VerifyAssetResult(
                    Type: type, Name: name, Path: assetPath,
                    Status: VerifyStatus.Modified,
                    ExpectedChecksum: expectedChecksum,
                    ActualChecksum: actualChecksum));
                warnings.Add($"{name} has been modified locally");
            }
        }

        return await Task.FromResult(VerifyResult.FromAssets(results, warnings: warnings));
    }

    private Task RestoreAssetAsync(string relativePath, string targetPath)
    {
        var templatesPath = _syncEngine.GetTemplatesPath();
        var templateFile = _fileSystem.CombinePath(templatesPath, relativePath);

        // Ensure directory exists
        var directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(directory))
            _fileSystem.CreateDirectory(directory);

        // Copy from template
        _fileSystem.CopyFile(templateFile, targetPath, overwrite: true);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<DryRunResult> PreviewInitAsync(InitOptions options)
    {
        var operations = new List<PlannedOperation>();
        var targetDir = _fileSystem.GetFullPath(options.TargetDirectory);
        var targetGitHubPath = _fileSystem.CombinePath(targetDir, ".github");
        var manifestPath = _fileSystem.CombinePath(targetDir, Manifest.RelativePath);

        // Check if already initialized
        var existingManifest = _syncEngine.ReadManifest(targetDir);
        if (existingManifest != null && !options.Force)
        {
            operations.Add(new PlannedOperation(
                OperationType.Skip,
                targetGitHubPath,
                "Already initialized. Use --force to overwrite."));
            return await Task.FromResult(DryRunResult.FromOperations(operations));
        }

        // Get templates and preview what would be created
        var templatesPath = _syncEngine.GetTemplatesPath();
        if (!_fileSystem.Exists(templatesPath))
        {
            operations.Add(new PlannedOperation(
                OperationType.Skip,
                templatesPath,
                "Templates directory not found"));
            return await Task.FromResult(DryRunResult.FromOperations(operations));
        }

        var templateFiles = _fileSystem.GetFiles(templatesPath, "*", recursive: true);

        foreach (var templateFile in templateFiles)
        {
            var relativePath = Path.GetRelativePath(templatesPath, templateFile);

            // Apply filter
            if (options.Filter != null && !options.Filter.ShouldIncludePath(relativePath))
                continue;

            var targetPath = _fileSystem.CombinePath(targetGitHubPath, relativePath);

            if (_fileSystem.Exists(targetPath))
            {
                if (options.Force)
                {
                    var sourceChecksum = _fileSystem.ComputeChecksum(templateFile);
                    var targetChecksum = _fileSystem.ComputeChecksum(targetPath);

                    if (sourceChecksum != targetChecksum)
                    {
                        operations.Add(new PlannedOperation(
                            OperationType.Update,
                            relativePath,
                            "content differs"));
                    }
                    else
                    {
                        operations.Add(new PlannedOperation(
                            OperationType.Skip,
                            relativePath,
                            "unchanged"));
                    }
                }
                else
                {
                    operations.Add(new PlannedOperation(
                        OperationType.Skip,
                        relativePath,
                        "exists"));
                }
            }
            else
            {
                operations.Add(new PlannedOperation(
                    OperationType.Create,
                    relativePath));
            }
        }

        // Check gitignore
        var gitignorePath = _fileSystem.CombinePath(targetDir, ".gitignore");
        if (_fileSystem.Exists(gitignorePath))
        {
            var content = _fileSystem.ReadAllText(gitignorePath);
            if (!content.Contains(".copilot-assets.json"))
            {
                operations.Add(new PlannedOperation(
                    OperationType.Modify,
                    ".gitignore",
                    "append entry"));
            }
        }

        // Manifest
        if (!_fileSystem.Exists(manifestPath))
        {
            operations.Add(new PlannedOperation(
                OperationType.Create,
                Manifest.FileName));
        }
        else if (options.Force)
        {
            operations.Add(new PlannedOperation(
                OperationType.Update,
                Manifest.FileName,
                "update manifest"));
        }

        return await Task.FromResult(DryRunResult.FromOperations(operations));
    }

    /// <inheritdoc />
    public async Task<DryRunResult> PreviewUpdateAsync(UpdateOptions options)
    {
        var operations = new List<PlannedOperation>();
        var targetDir = _fileSystem.GetFullPath(options.TargetDirectory);

        // Check if initialized
        var existingManifest = _syncEngine.ReadManifest(targetDir);
        if (existingManifest == null)
        {
            operations.Add(new PlannedOperation(
                OperationType.Skip,
                ".",
                "Assets not installed. Run 'copilot-assets init' first."));
            return await Task.FromResult(DryRunResult.FromOperations(operations));
        }

        // Check for updates using checksum comparison
        var updateCheck = await _syncEngine.CheckForUpdatesAsync(targetDir);
        if (updateCheck.NotInstalled)
        {
            operations.Add(new PlannedOperation(
                OperationType.Skip,
                ".",
                "Assets not installed. Run 'copilot-assets init' first."));
            return await Task.FromResult(DryRunResult.FromOperations(operations));
        }

        if (!updateCheck.HasChanges && !options.Force)
        {
            operations.Add(new PlannedOperation(
                OperationType.Skip,
                ".",
                "Templates unchanged. Use --force to reinstall."));
            return await Task.FromResult(DryRunResult.FromOperations(operations));
        }

        // Preview update like init with force
        var initOptions = new InitOptions
        {
            TargetDirectory = options.TargetDirectory,
            Force = true,
            Filter = options.Filter
        };

        return await PreviewInitAsync(initOptions);
    }

    /// <summary>
    /// Prompt user for confirmation before committing changes.
    /// </summary>
    private bool PromptUserForCommit(int fileCount)
    {
        Console.WriteLine();
        Console.WriteLine($"Ready to commit {fileCount} file(s) to git.");
        Console.Write("Do you want to commit these changes? [Y/n]: ");

        var response = Console.ReadLine()?.Trim().ToLowerInvariant();

        // Default to Yes if user just presses Enter
        return string.IsNullOrEmpty(response) || response == "y" || response == "yes";
    }

    /// <inheritdoc />
    public Task<(List<PendingFile> Files, string? Source, string? Error)> GetPendingOperationsAsync(
        string targetDirectory,
        AssetTypeFilter? filter = null,
        string? sourceOverride = null,
        CancellationToken ct = default)
    {
        var targetDir = _fileSystem.GetFullPath(targetDirectory);
        return _syncEngine.GetPendingOperationsAsync(targetDir, filter, sourceOverride, ct);
    }

    /// <inheritdoc />
    public ValidationResult ExecuteSelectiveSync(
        string targetDirectory,
        IEnumerable<PendingFile> selectedFiles,
        string? source = null,
        bool noGit = false)
    {
        var result = new ValidationResult();
        var targetDir = _fileSystem.GetFullPath(targetDirectory);
        var filesList = selectedFiles.ToList();

        if (filesList.Count == 0)
        {
            result.Info.Add("No files selected for installation.");
            return result;
        }

        // Execute selective sync
        var syncResult = _syncEngine.ExecuteSelective(targetDir, filesList, source);

        if (!syncResult.Success)
        {
            result.Errors.AddRange(syncResult.Errors);
            return result;
        }

        // Update .gitignore to ensure Copilot assets are ignored
        if (!noGit && _git.IsRepository(targetDir))
        {
            _git.EnsureGitignoreIgnoresCopilotAssets(targetDir);
        }

        // Git operations
        if (!noGit && _git.IsRepository(targetDir))
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

            // Prompt user for confirmation
            if (PromptUserForCommit(syncResult.Synced.Count))
            {
                _git.Stage(targetDir, filesToStage.ToArray());
                _git.Commit(targetDir, "chore: install copilot assets");
                result.Info.Add("Changes committed to git");
            }
            else
            {
                result.Info.Add("Changes not committed. Files are ready to be staged.");
            }
        }

        result.Info.Add($"✓ Installed {syncResult.Synced.Count} asset(s)");
        result.Warnings.AddRange(syncResult.Warnings);

        return result;
    }
}
