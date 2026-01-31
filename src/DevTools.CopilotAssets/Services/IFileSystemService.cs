namespace DevTools.CopilotAssets.Services;

/// <summary>
/// File system operations abstraction.
/// </summary>
public interface IFileSystemService
{
    /// <summary>
    /// Check if a file or directory exists.
    /// </summary>
    bool Exists(string path);

    /// <summary>
    /// Check if path is a directory.
    /// </summary>
    bool IsDirectory(string path);

    /// <summary>
    /// Create directory (and parents if needed).
    /// </summary>
    void CreateDirectory(string path);

    /// <summary>
    /// Copy file from source to target.
    /// </summary>
    void CopyFile(string source, string target, bool overwrite = false);

    /// <summary>
    /// Read file contents as string.
    /// </summary>
    string ReadAllText(string path);

    /// <summary>
    /// Write string content to file.
    /// </summary>
    void WriteAllText(string path, string content);

    /// <summary>
    /// Get all files in directory matching pattern.
    /// </summary>
    IEnumerable<string> GetFiles(string directory, string pattern = "*", bool recursive = false);

    /// <summary>
    /// Get all directories in path.
    /// </summary>
    IEnumerable<string> GetDirectories(string path);

    /// <summary>
    /// Delete a file.
    /// </summary>
    void DeleteFile(string path);

    /// <summary>
    /// Compute SHA256 checksum of file.
    /// </summary>
    string ComputeChecksum(string path);

    /// <summary>
    /// Get the full/absolute path.
    /// </summary>
    string GetFullPath(string path);

    /// <summary>
    /// Combine path segments.
    /// </summary>
    string CombinePath(params string[] paths);
}
