namespace DevTools.CopilotAssets.Services.Templates;

/// <summary>
/// Represents a single template file.
/// </summary>
/// <param name="RelativePath">Path relative to .github/ folder (e.g., "copilot-instructions.md").</param>
/// <param name="Content">File content.</param>
public sealed record TemplateFile(string RelativePath, string Content)
{
    /// <summary>
    /// Get the full path when copying to a target directory.
    /// </summary>
    /// <param name="targetGitHubDir">The .github directory in the target project.</param>
    /// <returns>Full path to write the file.</returns>
    public string GetTargetPath(string targetGitHubDir)
    {
        return Path.Combine(targetGitHubDir, RelativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
