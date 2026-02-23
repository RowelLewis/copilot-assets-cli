using System.Text.RegularExpressions;
using DevTools.CopilotAssets.Domain;
using DevTools.CopilotAssets.Infrastructure.Security;
using DevTools.CopilotAssets.Services.Skills;

namespace DevTools.CopilotAssets.Services;

/// <summary>
/// Validates project compliance with Copilot asset policies.
/// </summary>
public sealed class ValidationEngine
{
    private readonly IFileSystemService _fileSystem;
    private readonly SyncEngine _syncEngine;

    // Default policy - can be overridden via policy.json in future
    private static readonly PolicyDefinition DefaultPolicy = new()
    {
        RequiredFiles =
        [
            "copilot-instructions.md"
        ],
        RestrictedPatterns =
        [
            // Common secret patterns
            @"(?i)(api[_-]?key|apikey)\s*[:=]\s*['""]?[a-zA-Z0-9]{20,}",
            @"(?i)(secret|password|passwd|pwd)\s*[:=]\s*['""]?[^\s'"",]{8,}",
            @"(?i)bearer\s+[a-zA-Z0-9\-_.~+/]+=*",
            @"-----BEGIN\s+(RSA\s+)?PRIVATE\s+KEY-----"
        ],
        EnforceInCi = true
    };

    public ValidationEngine(IFileSystemService fileSystem, SyncEngine syncEngine)
    {
        _fileSystem = fileSystem;
        _syncEngine = syncEngine;
    }

    /// <summary>
    /// Validate the project against the policy.
    /// </summary>
    public ValidationResult Validate(string targetDirectory, bool strictMode = false)
    {
        var result = new ValidationResult();
        var gitHubPath = _fileSystem.CombinePath(targetDirectory, ".github");

        // Check if .github directory exists
        if (!_fileSystem.Exists(gitHubPath))
        {
            result.Errors.Add("Missing .github directory. Run 'copilot-assets init' to install assets.");
            return result;
        }

        // Check manifest
        var manifest = _syncEngine.ReadManifest(targetDirectory);
        if (manifest == null)
        {
            result.Errors.Add($"Missing manifest file ({Manifest.RelativePath}). Run 'copilot-assets init' to install assets.");
            return result;
        }

        // Required files check
        foreach (var requiredFile in DefaultPolicy.RequiredFiles)
        {
            var filePath = _fileSystem.CombinePath(gitHubPath, requiredFile);
            if (!_fileSystem.Exists(filePath))
            {
                result.Errors.Add($"Missing required file: .github/{requiredFile}");
            }
        }

        // Checksum verification
        foreach (var (assetPath, expectedChecksum) in manifest.Checksums)
        {
            var resolvedPath = AssetPathResolver.ResolveToFileSystemPath(assetPath);
            var fullPath = _fileSystem.CombinePath(targetDirectory, resolvedPath);
            if (_fileSystem.Exists(fullPath))
            {
                var actualChecksum = _fileSystem.ComputeChecksum(fullPath);
                if (actualChecksum != expectedChecksum)
                {
                    if (strictMode)
                    {
                        result.Errors.Add($"File modified: {resolvedPath} (checksum mismatch)");
                    }
                    else
                    {
                        result.Warnings.Add($"File modified locally: {resolvedPath}");
                    }
                }
            }
            else
            {
                result.Errors.Add($"Missing asset: {resolvedPath}");
            }
        }

        // SKILL.md validation
        var skillFiles = _fileSystem.GetFiles(gitHubPath, "SKILL.md", recursive: true);
        foreach (var skillFile in skillFiles)
        {
            var skillContent = _fileSystem.ReadAllText(skillFile);
            var skillRelativePath = Path.GetRelativePath(targetDirectory, skillFile);
            var skillErrors = SkillParser.Validate(skillContent, skillRelativePath);
            foreach (var error in skillErrors)
            {
                result.Warnings.Add(error);
            }
        }

        // Restricted patterns check (security scan)
        var allFiles = _fileSystem.GetFiles(gitHubPath, "*", recursive: true)
            .Where(f => f.EndsWith(".md") || f.EndsWith(".json") || f.EndsWith(".yaml") || f.EndsWith(".yml"));

        foreach (var file in allFiles)
        {
            var content = _fileSystem.ReadAllText(file);
            var relativePath = Path.GetRelativePath(targetDirectory, file);

            foreach (var pattern in DefaultPolicy.RestrictedPatterns)
            {
                if (Regex.IsMatch(content, pattern))
                {
                    result.Errors.Add($"Potential secret detected in {relativePath}");
                    break; // One error per file is enough
                }
            }
        }

        if (result.IsCompliant)
        {
            result.Info.Add("✓ All validations passed");
        }

        return result;
    }
}
