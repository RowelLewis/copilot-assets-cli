using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using DevTools.CopilotAssets.Domain;
using DevTools.CopilotAssets.Services.Templates;

namespace DevTools.CopilotAssets.Services;

/// <summary>
/// Handles synchronization of assets from templates to target project.
/// </summary>
public sealed class SyncEngine
{
    private readonly IFileSystemService _fileSystem;
    private readonly IGitService _git;
    private readonly ITemplateProvider? _templateProvider;

    // Asset version embedded in the tool
    public static string AssetVersion => "1.0.0";

    // Tool version from assembly
    public static string ToolVersion =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

    /// <summary>
    /// Source description from the last template provider used.
    /// </summary>
    public string? LastTemplateSource { get; private set; }

    public SyncEngine(IFileSystemService fileSystem, IGitService git)
    {
        _fileSystem = fileSystem;
        _git = git;
    }

    public SyncEngine(IFileSystemService fileSystem, IGitService git, ITemplateProvider templateProvider)
        : this(fileSystem, git)
    {
        _templateProvider = templateProvider;
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

    /// <summary>
    /// Sync assets using the configured template provider.
    /// Falls back to bundled templates if no provider is configured.
    /// </summary>
    public async Task<SyncResult> SyncAssetsAsync(
        string targetDirectory,
        bool force = false,
        AssetTypeFilter? filter = null,
        CancellationToken ct = default)
    {
        // If no template provider, use the synchronous bundled path
        if (_templateProvider == null)
        {
            return SyncAssets(targetDirectory, force, filter);
        }

        var result = new SyncResult();
        var targetGitHubPath = _fileSystem.CombinePath(targetDirectory, ".github");

        // Get templates from the provider
        var templateResult = await _templateProvider.GetTemplatesAsync(ct);
        LastTemplateSource = templateResult.Source;

        if (templateResult.HasError && !templateResult.HasTemplates)
        {
            result.Errors.Add(templateResult.Error!);
            return result;
        }

        if (!templateResult.HasTemplates)
        {
            result.Errors.Add("No templates available");
            return result;
        }

        // Add warning if there was an error but we got cached/fallback templates
        if (templateResult.HasError)
        {
            result.Warnings.Add(templateResult.Error!);
        }

        // Create .github directory if it doesn't exist
        _fileSystem.CreateDirectory(targetGitHubPath);

        foreach (var template in templateResult.Templates)
        {
            ct.ThrowIfCancellationRequested();

            var relativePath = template.RelativePath;

            // Apply filter if specified
            if (filter != null && !filter.ShouldIncludePath(relativePath))
            {
                result.Skipped.Add(relativePath);
                continue;
            }

            var targetFile = template.GetTargetPath(targetGitHubPath);
            var targetDir = Path.GetDirectoryName(targetFile);
            if (!string.IsNullOrEmpty(targetDir))
            {
                _fileSystem.CreateDirectory(targetDir);
            }

            var exists = _fileSystem.Exists(targetFile);

            if (exists && !force)
            {
                // Check if content is different
                var sourceChecksum = ComputeChecksum(template.Content);
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
                _fileSystem.WriteAllText(targetFile, template.Content);
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
    /// Get list of all files that would be synced using the template provider.
    /// </summary>
    public async Task<IEnumerable<string>> GetAssetListAsync(CancellationToken ct = default)
    {
        if (_templateProvider == null)
        {
            return GetAssetList();
        }

        var templateResult = await _templateProvider.GetTemplatesAsync(ct);
        LastTemplateSource = templateResult.Source;

        return templateResult.Templates.Select(t => t.RelativePath);
    }

    /// <summary>
    /// Compute checksum of content string.
    /// </summary>
    private static string ComputeChecksum(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
