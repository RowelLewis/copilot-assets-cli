using System.Security.Cryptography;

namespace DevTools.CopilotAssets.Services;

/// <summary>
/// File system operations implementation.
/// </summary>
public sealed class FileSystemService : IFileSystemService
{
    public bool Exists(string path) =>
        File.Exists(path) || Directory.Exists(path);

    public bool IsDirectory(string path) =>
        Directory.Exists(path);

    public void CreateDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    public void CopyFile(string source, string target, bool overwrite = false)
    {
        var targetDir = Path.GetDirectoryName(target);
        if (!string.IsNullOrEmpty(targetDir))
        {
            CreateDirectory(targetDir);
        }
        File.Copy(source, target, overwrite);
    }

    public string ReadAllText(string path) =>
        File.ReadAllText(path);

    public void WriteAllText(string path, string content)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            CreateDirectory(dir);
        }
        File.WriteAllText(path, content);
    }

    public IEnumerable<string> GetFiles(string directory, string pattern = "*", bool recursive = false) =>
        Directory.Exists(directory)
            ? Directory.GetFiles(directory, pattern, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
            : [];

    public IEnumerable<string> GetDirectories(string path) =>
        Directory.Exists(path)
            ? Directory.GetDirectories(path)
            : [];

    public void DeleteFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public string ComputeChecksum(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public string GetFullPath(string path) =>
        Path.GetFullPath(path);

    public string CombinePath(params string[] paths) =>
        Path.Combine(paths);
}
