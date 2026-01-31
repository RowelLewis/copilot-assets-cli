using System.Text.RegularExpressions;
using DevTools.CopilotAssets.Domain;

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
        MinimumVersion = "1.0.0",
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

        // Version check
        if (!IsVersionSatisfied(manifest.Version, DefaultPolicy.MinimumVersion))
        {
            result.Errors.Add($"Asset version {manifest.Version} is below minimum required {DefaultPolicy.MinimumVersion}. Run 'copilot-assets update'.");
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
            var fullPath = _fileSystem.CombinePath(gitHubPath, assetPath);
            if (_fileSystem.Exists(fullPath))
            {
                var actualChecksum = _fileSystem.ComputeChecksum(fullPath);
                if (actualChecksum != expectedChecksum)
                {
                    if (strictMode)
                    {
                        result.Errors.Add($"File modified: .github/{assetPath} (checksum mismatch)");
                    }
                    else
                    {
                        result.Warnings.Add($"File modified locally: .github/{assetPath}");
                    }
                }
            }
            else
            {
                result.Errors.Add($"Missing asset: .github/{assetPath}");
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
            result.Info.Add($"✓ All validations passed (version {manifest.Version})");
        }

        return result;
    }

    /// <summary>
    /// Compare semantic versions.
    /// </summary>
    private static bool IsVersionSatisfied(string installed, string minimum)
    {
        if (Version.TryParse(installed, out var installedVer) &&
            Version.TryParse(minimum, out var minimumVer))
        {
            return installedVer >= minimumVer;
        }
        return true; // If parsing fails, assume satisfied
    }
}
