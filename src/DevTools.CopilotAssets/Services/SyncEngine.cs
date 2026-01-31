using System.Reflection;
using DevTools.CopilotAssets.Domain;

namespace DevTools.CopilotAssets.Services;

/// <summary>
/// Handles synchronization of assets from templates to target project.
/// </summary>
public sealed class SyncEngine
{
    private readonly IFileSystemService _fileSystem;
    private readonly IGitService _git;

    // Asset version embedded in the tool
    public static string AssetVersion => "1.0.0";

    // Tool version from assembly
    public static string ToolVersion =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

    public SyncEngine(IFileSystemService fileSystem, IGitService git)
    {
        _fileSystem = fileSystem;
        _git = git;
    }

    /// <summary>
    /// Get the templates directory path (relative to executable).
    /// </summary>
    public string GetTemplatesPath()
    {
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? Environment.CurrentDirectory;
        return _fileSystem.CombinePath(assemblyDir, "templates", ".github");
    }

    /// <summary>
    /// Sync all assets to the target directory.
    /// </summary>
    public SyncResult SyncAssets(string targetDirectory, bool force = false, AssetTypeFilter? filter = null)
    {
        var result = new SyncResult();
        var templatesPath = GetTemplatesPath();
        var targetGitHubPath = _fileSystem.CombinePath(targetDirectory, ".github");

        if (!_fileSystem.Exists(templatesPath))
        {
            result.Errors.Add($"Templates directory not found: {templatesPath}");
            return result;
        }

        // Create .github directory if it doesn't exist
        _fileSystem.CreateDirectory(targetGitHubPath);

        // Sync all files from templates
        var templateFiles = _fileSystem.GetFiles(templatesPath, "*", recursive: true);

        foreach (var templateFile in templateFiles)
        {
            var relativePath = Path.GetRelativePath(templatesPath, templateFile);

            // Apply filter if specified
            if (filter != null && !filter.ShouldIncludePath(relativePath))
            {
                result.Skipped.Add(relativePath);
                continue;
            }

            var targetFile = _fileSystem.CombinePath(targetGitHubPath, relativePath);

            var exists = _fileSystem.Exists(targetFile);

            if (exists && !force)
            {
                // Check if content is different
                var sourceChecksum = _fileSystem.ComputeChecksum(templateFile);
                var targetChecksum = _fileSystem.ComputeChecksum(targetFile);

                if (sourceChecksum != targetChecksum)
                {
                    result.Skipped.Add(relativePath);
                    result.Warnings.Add($"File exists with different content (use --force to overwrite): {relativePath}");
                }
                else
                {
                    result.Unchanged.Add(relativePath);
                }
            }
            else
            {
                _fileSystem.CopyFile(templateFile, targetFile, overwrite: force);
                var checksum = _fileSystem.ComputeChecksum(targetFile);

                result.Synced.Add(new SyncedAsset
                {
                    RelativePath = relativePath,
                    FullPath = targetFile,
                    Checksum = checksum,
                    WasUpdated = exists
                });
            }
        }

        // Write manifest
        WriteManifest(targetDirectory, result);

        return result;
    }

    /// <summary>
    /// Write the manifest file tracking installed assets.
    /// </summary>
    private void WriteManifest(string targetDirectory, SyncResult syncResult)
    {
        var manifest = Manifest.Create(AssetVersion, ToolVersion);

        foreach (var synced in syncResult.Synced)
        {
            manifest.Assets.Add(synced.RelativePath);
            manifest.Checksums[synced.RelativePath] = synced.Checksum;
        }

        foreach (var unchanged in syncResult.Unchanged)
        {
            manifest.Assets.Add(unchanged);
            var fullPath = _fileSystem.CombinePath(targetDirectory, ".github", unchanged);
            if (_fileSystem.Exists(fullPath))
            {
                manifest.Checksums[unchanged] = _fileSystem.ComputeChecksum(fullPath);
            }
        }

        var manifestPath = _fileSystem.CombinePath(targetDirectory, Manifest.RelativePath);
        _fileSystem.WriteAllText(manifestPath, manifest.ToJson());

        syncResult.Synced.Add(new SyncedAsset
        {
            RelativePath = Manifest.FileName,
            FullPath = manifestPath,
            Checksum = _fileSystem.ComputeChecksum(manifestPath),
            WasUpdated = false
        });
    }

    /// <summary>
    /// Read the existing manifest from a project.
    /// </summary>
    public Manifest? ReadManifest(string targetDirectory)
    {
        var manifestPath = _fileSystem.CombinePath(targetDirectory, Manifest.RelativePath);
        if (!_fileSystem.Exists(manifestPath))
        {
            return null;
        }

        var json = _fileSystem.ReadAllText(manifestPath);
        return Manifest.FromJson(json);
    }

    /// <summary>
    /// Get list of all files that would be synced.
    /// </summary>
    public IEnumerable<string> GetAssetList()
    {
        var templatesPath = GetTemplatesPath();
        if (!_fileSystem.Exists(templatesPath))
        {
            return [];
        }

        return _fileSystem.GetFiles(templatesPath, "*", recursive: true)
            .Select(f => Path.GetRelativePath(templatesPath, f));
    }
}
