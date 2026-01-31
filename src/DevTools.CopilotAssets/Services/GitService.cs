using LibGit2Sharp;

namespace DevTools.CopilotAssets.Services;

/// <summary>
/// Git operations implementation using LibGit2Sharp.
/// </summary>
public sealed class GitService : IGitService
{
    private readonly IFileSystemService _fileSystem;

    // Patterns to add to .gitignore to ensure Copilot assets are tracked
    private static readonly string[] CopilotAssetNegationPatterns =
    [
        "# GitHub Copilot Assets - DO NOT IGNORE",
        "!.github/copilot-instructions.md",
        "!.github/prompts/",
        "!.github/prompts/**",
        "!.github/agents/",
        "!.github/agents/**",
        "!.github/skills/",
        "!.github/skills/**",
        "!.github/.copilot-assets.json"
    ];

    public GitService(IFileSystemService fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public bool IsGitAvailable()
    {
        try
        {
            // LibGit2Sharp is available if we can call GlobalSettings
            _ = GlobalSettings.Version;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool IsRepository(string path)
    {
        try
        {
            return Repository.IsValid(path) || GetRepositoryRoot(path) != null;
        }
        catch
        {
            return false;
        }
    }

    public bool IsClean(string path)
    {
        var repoRoot = GetRepositoryRoot(path);
        if (repoRoot == null) return true;

        try
        {
            using var repo = new Repository(repoRoot);
            var status = repo.RetrieveStatus();
            return !status.IsDirty;
        }
        catch
        {
            return true;
        }
    }

    public void Stage(string repoPath, params string[] filePaths)
    {
        var repoRoot = GetRepositoryRoot(repoPath);
        if (repoRoot == null) return;

        using var repo = new Repository(repoRoot);
        foreach (var filePath in filePaths)
        {
            // Convert to relative path from repo root
            var relativePath = Path.GetRelativePath(repoRoot, filePath);
            LibGit2Sharp.Commands.Stage(repo, relativePath);
        }
    }

    public void Commit(string repoPath, string message)
    {
        var repoRoot = GetRepositoryRoot(repoPath);
        if (repoRoot == null) return;

        using var repo = new Repository(repoRoot);

        // Get or create signature
        var signature = repo.Config.BuildSignature(DateTimeOffset.Now);
        if (signature == null)
        {
            signature = new Signature("Copilot Assets CLI", "copilot-assets@local", DateTimeOffset.Now);
        }

        repo.Commit(message, signature, signature);
    }

    public void EnsureGitignoreAllowsCopilotAssets(string repoPath)
    {
        var repoRoot = GetRepositoryRoot(repoPath);
        if (repoRoot == null) return;

        var gitignorePath = _fileSystem.CombinePath(repoRoot, ".gitignore");
        var existingContent = _fileSystem.Exists(gitignorePath)
            ? _fileSystem.ReadAllText(gitignorePath)
            : "";

        var lines = existingContent.Split('\n').ToList();
        var modified = false;

        foreach (var pattern in CopilotAssetNegationPatterns)
        {
            if (!lines.Any(l => l.Trim() == pattern.Trim()))
            {
                lines.Add(pattern);
                modified = true;
            }
        }

        if (modified)
        {
            // Ensure there's a blank line before our section if content exists
            if (!string.IsNullOrWhiteSpace(existingContent) && !existingContent.EndsWith('\n'))
            {
                lines.Insert(lines.Count - CopilotAssetNegationPatterns.Length, "");
            }

            _fileSystem.WriteAllText(gitignorePath, string.Join('\n', lines));
        }
    }

    public string? GetRepositoryRoot(string path)
    {
        try
        {
            var fullPath = _fileSystem.GetFullPath(path);
            var repoPath = Repository.Discover(fullPath);
            if (string.IsNullOrEmpty(repoPath)) return null;

            // Repository.Discover returns path to .git folder, we need the parent
            using var repo = new Repository(repoPath);
            return repo.Info.WorkingDirectory?.TrimEnd(Path.DirectorySeparatorChar);
        }
        catch
        {
            return null;
        }
    }
}
