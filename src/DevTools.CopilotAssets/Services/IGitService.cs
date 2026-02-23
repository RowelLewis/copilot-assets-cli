namespace DevTools.CopilotAssets.Services;

/// <summary>
/// Git operations abstraction using LibGit2Sharp.
/// </summary>
public interface IGitService
{
    /// <summary>
    /// Check if Git is available on the system.
    /// </summary>
    bool IsGitAvailable();

    /// <summary>
    /// Check if the path is inside a Git repository.
    /// </summary>
    bool IsRepository(string path);

    /// <summary>
    /// Ensure .gitignore ignores Copilot assets.
    /// </summary>
    void EnsureGitignoreIgnoresCopilotAssets(string repoPath);

    /// <summary>
    /// Get the repository root directory.
    /// </summary>
    string? GetRepositoryRoot(string path);
}
