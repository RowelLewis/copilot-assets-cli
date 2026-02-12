using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using DevTools.CopilotAssets.Domain;
using DevTools.CopilotAssets.Infrastructure.Security;
using DevTools.CopilotAssets.Services.Adapters;
using DevTools.CopilotAssets.Services.Results;
using DevTools.CopilotAssets.Services.Templates;

namespace DevTools.CopilotAssets.Services;

/// <summary>
/// Represents a pending file operation for interactive mode.
/// </summary>
public sealed record PendingFile(
    string RelativePath,
    string Folder,
    PendingFileStatus Status,
    string Content)
{
    /// <summary>
    /// Get the top-level folder or empty string for root files.
    /// For skills, returns the skill folder path (e.g., "skills/refactor").
    /// </summary>
    public static string GetFolder(string relativePath)
    {
        var normalized = relativePath.Replace("\\", "/");
        var parts = normalized.Split('/');

        // For skills, group by skill folder (e.g., "skills/refactor")
        if (parts.Length >= 3 && parts[0] == "skills" && parts[2] == "SKILL.md")
        {
            return $"skills/{parts[1]}";
        }

        // For other assets, return top-level folder
        var separatorIndex = relativePath.IndexOfAny(['/', '\\']);
        return separatorIndex > 0 ? relativePath[..separatorIndex] : "";
    }

    /// <summary>
    /// Get display name for a folder (extracts skill name for skills, friendly names for others).
    /// </summary>
    public static string GetFolderDisplayName(string folder)
    {
        if (folder.StartsWith("skills/"))
        {
            var skillName = folder.Substring("skills/".Length);
            return $"{skillName} skill";
        }

        // For instructions folder, keep it simple
        if (folder == "instructions")
        {
            return "custom instructions";
        }

        return folder;
    }
}

/// <summary>
/// Status of a pending file operation.
/// </summary>
public enum PendingFileStatus
{
    New,
    Modified,
    Unchanged
}

/// <summary>
/// Handles synchronization of assets from templates to target project.
/// </summary>
public sealed class SyncEngine
{
    private readonly IFileSystemService _fileSystem;
    private readonly IGitService _git;
    private readonly ITemplateProvider? _templateProvider;
    private readonly TemplateProviderFactory? _providerFactory;
    private readonly OutputAdapterFactory _adapterFactory;

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
        _adapterFactory = new OutputAdapterFactory();
    }

    public SyncEngine(IFileSystemService fileSystem, IGitService git, ITemplateProvider templateProvider)
        : this(fileSystem, git)
    {
        _templateProvider = templateProvider;
    }

    public SyncEngine(IFileSystemService fileSystem, IGitService git, TemplateProviderFactory providerFactory)
        : this(fileSystem, git)
    {
        _providerFactory = providerFactory;
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
    public SyncResult SyncAssets(string targetDirectory, bool force = false, AssetTypeFilter? filter = null, IReadOnlyList<TargetTool>? targets = null)
    {
        var result = new SyncResult();
        var templatesPath = GetTemplatesPath();
        var targetGitHubPath = _fileSystem.CombinePath(targetDirectory, ".github");
        var effectiveTargets = targets ?? [TargetTool.Copilot];

        if (!_fileSystem.Exists(templatesPath))
        {
            result.Errors.Add($"Templates directory not found: {templatesPath}");
            return result;
        }

        // Create .github directory if it doesn't exist (always needed for manifest)
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

            var content = _fileSystem.ReadAllText(templateFile);
            var assetType = GetAssetType(relativePath);
            var fileName = Path.GetFileName(relativePath);

            // Sync to each target tool
            SyncToTargets(targetDirectory, relativePath, content, assetType, fileName, force, effectiveTargets, result);
        }

        // Write manifest (default source for synchronous path)
        WriteManifest(targetDirectory, result, "default", effectiveTargets);

        return result;
    }

    /// <summary>
    /// Write the manifest file tracking installed assets.
    /// </summary>
    private void WriteManifest(string targetDirectory, SyncResult syncResult, string? sourceString = null, IReadOnlyList<TargetTool>? targets = null)
    {
        var source = sourceString != null
            ? TemplateSource.Parse(sourceString)
            : TemplateSource.Default();

        var manifest = Manifest.Create(ToolVersion, source, targets);

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
        var manifest = Manifest.FromJson(json);

        // Validate manifest for security issues
        if (manifest != null)
        {
            try
            {
                ManifestValidator.Validate(manifest);
            }
            catch (SecurityException ex)
            {
                Console.Error.WriteLine($"Warning: Manifest validation failed: {ex.Message}");
                // Return null to force re-initialization
                return null;
            }
        }

        return manifest;
    }

    /// <summary>
    /// Check for updates by comparing checksums between installed and available templates.
    /// </summary>
    public async Task<UpdateCheckResult> CheckForUpdatesAsync(
        string targetDirectory,
        AssetTypeFilter? filter = null,
        string? sourceOverride = null,
        CancellationToken ct = default)
    {
        var result = new UpdateCheckResult();
        var manifest = ReadManifest(targetDirectory);

        if (manifest == null)
        {
            result.NotInstalled = true;
            return result;
        }

        // Get templates from provider
        var provider = GetProvider(sourceOverride);
        var templateResult = provider != null
            ? await provider.GetTemplatesAsync(ct)
            : await GetDefaultTemplatesAsync(ct);

        LastTemplateSource = templateResult.Source;

        if (!templateResult.HasTemplates)
        {
            result.Error = templateResult.Error ?? "No templates available";
            return result;
        }

        // Build a mapping from template paths to their tracking paths (as stored in manifest)
        var copilotAdapter = _adapterFactory.CreateAdapter(TargetTool.Copilot);
        var templateToTrackingPath = new Dictionary<string, string>();

        // Compare each template against installed checksums
        foreach (var template in templateResult.Templates)
        {
            if (filter != null && !filter.ShouldIncludePath(template.RelativePath))
                continue;

            var assetType = GetAssetType(template.RelativePath);
            var outputPath = copilotAdapter.GetOutputPath(assetType, template.RelativePath);

            // Derive tracking path the same way SyncToTargets does
            var trackingPath = outputPath.StartsWith(".github/") || outputPath.StartsWith(".github\\")
                ? outputPath[".github/".Length..]
                : outputPath.StartsWith($".github{Path.DirectorySeparatorChar}")
                    ? outputPath[(".github" + Path.DirectorySeparatorChar).Length..]
                    : template.RelativePath;

            templateToTrackingPath[template.RelativePath] = trackingPath;

            var newChecksum = ComputeChecksum(template.Content);
            var installedChecksum = manifest.Checksums.GetValueOrDefault(trackingPath);

            if (installedChecksum == null)
            {
                result.Added.Add(trackingPath);
            }
            else if (installedChecksum != newChecksum)
            {
                result.Modified.Add(trackingPath);
            }
            else
            {
                result.Unchanged.Add(trackingPath);
            }
        }

        // Check for removed files
        var trackingPaths = templateToTrackingPath.Values.ToHashSet();
        foreach (var asset in manifest.Assets.Where(a => a != Manifest.FileName))
        {
            if (filter != null && !filter.ShouldIncludePath(asset))
                continue;

            if (!trackingPaths.Contains(asset))
            {
                result.Removed.Add(asset);
            }
        }

        return result;
    }

    /// <summary>
    /// Get default templates as TemplateResult.
    /// </summary>
    private Task<TemplateResult> GetDefaultTemplatesAsync(CancellationToken ct)
    {
        var templatesPath = GetTemplatesPath();
        if (!_fileSystem.Exists(templatesPath))
        {
            return Task.FromResult(TemplateResult.Failed("default", "Templates directory not found"));
        }

        var templates = new List<TemplateFile>();
        var templateFiles = _fileSystem.GetFiles(templatesPath, "*", recursive: true);

        foreach (var file in templateFiles)
        {
            ct.ThrowIfCancellationRequested();
            var relativePath = Path.GetRelativePath(templatesPath, file);
            var content = _fileSystem.ReadAllText(file);
            templates.Add(new TemplateFile(relativePath, content));
        }

        return Task.FromResult(new TemplateResult(templates, "default"));
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
    /// Get the template provider to use, based on source override or factory.
    /// </summary>
    private ITemplateProvider? GetProvider(string? sourceOverride)
    {
        // If a source override is specified and we have a factory, create provider on-demand
        if (!string.IsNullOrEmpty(sourceOverride) && _providerFactory != null)
        {
            return _providerFactory.CreateFromSource(sourceOverride);
        }

        // If we have an injected provider, use it
        if (_templateProvider != null)
        {
            return _templateProvider;
        }

        // If we have a factory but no override, use config
        if (_providerFactory != null)
        {
            return _providerFactory.CreateFromConfig();
        }

        // No provider available - will fall back to bundled
        return null;
    }

    /// <summary>
    /// Sync assets using the configured template provider.
    /// Falls back to bundled templates if no provider is configured.
    /// </summary>
    public async Task<SyncResult> SyncAssetsAsync(
        string targetDirectory,
        bool force = false,
        AssetTypeFilter? filter = null,
        string? sourceOverride = null,
        IReadOnlyList<TargetTool>? targets = null,
        CancellationToken ct = default)
    {
        var provider = GetProvider(sourceOverride);
        var effectiveTargets = targets ?? [TargetTool.Copilot];

        // If no template provider, use the synchronous bundled path
        if (provider == null)
        {
            return SyncAssets(targetDirectory, force, filter, effectiveTargets);
        }

        var result = new SyncResult();
        var targetGitHubPath = _fileSystem.CombinePath(targetDirectory, ".github");

        // Get templates from the provider
        var templateResult = await provider.GetTemplatesAsync(ct);
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

        // Create .github directory if it doesn't exist (always needed for manifest)
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

            var assetType = GetAssetType(relativePath);
            var fileName = Path.GetFileName(relativePath);

            // Sync to each target tool
            SyncToTargets(targetDirectory, relativePath, template.Content, assetType, fileName, force, effectiveTargets, result);
        }

        // Write manifest with source from template provider
        WriteManifest(targetDirectory, result, templateResult.Source, effectiveTargets);

        return result;
    }

    /// <summary>
    /// Sync a single template to all target tools.
    /// </summary>
    private void SyncToTargets(
        string targetDirectory,
        string templateRelativePath,
        string content,
        AssetType assetType,
        string fileName,
        bool force,
        IReadOnlyList<TargetTool> targets,
        SyncResult result)
    {
        var adapters = _adapterFactory.CreateAdapters(targets);

        foreach (var adapter in adapters)
        {
            var metadata = new AssetMetadata
            {
                FileName = fileName,
                RelativePath = templateRelativePath,
                Type = assetType
            };

            var transformedContent = adapter.TransformContent(content, metadata);
            var outputPath = adapter.GetOutputPath(assetType, templateRelativePath);
            var targetFile = _fileSystem.CombinePath(targetDirectory, outputPath);

            // Ensure target directory exists
            var targetDir = Path.GetDirectoryName(targetFile);
            if (!string.IsNullOrEmpty(targetDir))
            {
                _fileSystem.CreateDirectory(targetDir);
            }

            var exists = _fileSystem.Exists(targetFile);

            // Use a combined key for tracking: "target:outputPath" for non-Copilot,
            // outputPath (relative to .github/) for Copilot
            var copilotPath = outputPath.StartsWith(".github/") || outputPath.StartsWith(".github\\")
                ? outputPath[".github/".Length..]
                : outputPath.StartsWith($".github{Path.DirectorySeparatorChar}")
                    ? outputPath[(".github" + Path.DirectorySeparatorChar).Length..]
                    : templateRelativePath;
            var trackingPath = adapter.Target == TargetTool.Copilot
                ? copilotPath
                : $"{adapter.Target.ToConfigName()}:{outputPath}";

            if (exists && !force)
            {
                var sourceChecksum = ComputeChecksum(transformedContent);
                var targetChecksum = _fileSystem.ComputeChecksum(targetFile);

                if (sourceChecksum != targetChecksum)
                {
                    result.Skipped.Add(trackingPath);
                    result.Warnings.Add($"File exists with different content (use --force to overwrite): {outputPath}");
                }
                else
                {
                    result.Unchanged.Add(trackingPath);
                }
            }
            else
            {
                _fileSystem.WriteAllText(targetFile, transformedContent);
                var checksum = ComputeChecksum(transformedContent);

                result.Synced.Add(new SyncedAsset
                {
                    RelativePath = trackingPath,
                    FullPath = targetFile,
                    Checksum = checksum,
                    WasUpdated = exists
                });
            }
        }
    }

    /// <summary>
    /// Determine the AssetType from a relative path.
    /// </summary>
    private static AssetType GetAssetType(string relativePath)
    {
        if (relativePath.Contains("prompts/") || relativePath.Contains("prompts\\"))
            return AssetType.Prompt;
        if (relativePath.Contains("agents/") || relativePath.Contains("agents\\"))
            return AssetType.Agent;
        if (relativePath.Contains("skills/") || relativePath.Contains("skills\\"))
            return AssetType.Skill;
        if (relativePath.Contains("instructions/") || relativePath.Contains("instructions\\"))
            return AssetType.Instruction;
        // Default: root-level instruction files (copilot-instructions.md, etc.)
        return AssetType.Instruction;
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
    internal static string ComputeChecksum(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Get pending operations without executing them (for interactive mode).
    /// </summary>
    public async Task<(List<PendingFile> Files, string? Source, string? Error)> GetPendingOperationsAsync(
        string targetDirectory,
        AssetTypeFilter? filter = null,
        string? sourceOverride = null,
        CancellationToken ct = default)
    {
        var targetGitHubPath = _fileSystem.CombinePath(targetDirectory, ".github");
        var provider = GetProvider(sourceOverride);

        // Get templates
        TemplateResult templateResult;
        if (provider != null)
        {
            templateResult = await provider.GetTemplatesAsync(ct);
        }
        else
        {
            templateResult = await GetDefaultTemplatesAsync(ct);
        }

        LastTemplateSource = templateResult.Source;

        if (templateResult.HasError && !templateResult.HasTemplates)
        {
            return ([], templateResult.Source, templateResult.Error);
        }

        if (!templateResult.HasTemplates)
        {
            return ([], templateResult.Source, "No templates available");
        }

        var pending = new List<PendingFile>();

        foreach (var template in templateResult.Templates)
        {
            ct.ThrowIfCancellationRequested();

            var relativePath = template.RelativePath;

            // Apply filter if specified
            if (filter != null && !filter.ShouldIncludePath(relativePath))
                continue;

            var targetFile = template.GetTargetPath(targetGitHubPath);
            var exists = _fileSystem.Exists(targetFile);

            PendingFileStatus status;
            if (!exists)
            {
                status = PendingFileStatus.New;
            }
            else
            {
                var sourceChecksum = ComputeChecksum(template.Content);
                var targetChecksum = _fileSystem.ComputeChecksum(targetFile);
                status = sourceChecksum != targetChecksum
                    ? PendingFileStatus.Modified
                    : PendingFileStatus.Unchanged;
            }

            var folder = PendingFile.GetFolder(relativePath);
            pending.Add(new PendingFile(relativePath, folder, status, template.Content));
        }

        return (pending, templateResult.Source, null);
    }

    /// <summary>
    /// Execute sync for only the selected files (for interactive mode).
    /// </summary>
    public SyncResult ExecuteSelective(
        string targetDirectory,
        IEnumerable<PendingFile> selectedFiles,
        string? sourceString = null,
        IReadOnlyList<TargetTool>? targets = null)
    {
        var result = new SyncResult();
        var effectiveTargets = targets ?? [TargetTool.Copilot];
        var targetGitHubPath = _fileSystem.CombinePath(targetDirectory, ".github");

        // Create .github directory if it doesn't exist
        _fileSystem.CreateDirectory(targetGitHubPath);

        foreach (var file in selectedFiles)
        {
            var assetType = GetAssetType(file.RelativePath);
            var fileName = Path.GetFileName(file.RelativePath);

            SyncToTargets(targetDirectory, file.RelativePath, file.Content, assetType, fileName, true, effectiveTargets, result);
        }

        // Write manifest
        WriteManifest(targetDirectory, result, sourceString ?? "default", effectiveTargets);

        return result;
    }
}
