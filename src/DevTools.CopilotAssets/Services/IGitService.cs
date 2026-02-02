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
    /// Check if the working directory is clean (no uncommitted changes).
    /// </summary>
    bool IsClean(string path);

    /// <summary>
    /// Stage files for commit.
    /// </summary>
    void Stage(string repoPath, params string[] filePaths);

    /// <summary>
    /// Commit staged changes.
    /// </summary>
    void Commit(string repoPath, string message);

    /// <summary>
    /// Ensure .gitignore ignores Copilot assets.
    /// </summary>
    void EnsureGitignoreIgnoresCopilotAssets(string repoPath);

    /// <summary>
    /// Get the repository root directory.
    /// </summary>
    string? GetRepositoryRoot(string path);
}
