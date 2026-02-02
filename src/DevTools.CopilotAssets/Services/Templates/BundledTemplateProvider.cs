using System.Reflection;

namespace DevTools.CopilotAssets.Services.Templates;

/// <summary>
/// Provides default templates bundled with the CLI tool.
/// This is the default provider when no remote source is configured.
/// </summary>
public sealed class BundledTemplateProvider : ITemplateProvider
{
    private readonly IFileSystemService _fileSystem;

    public BundledTemplateProvider(IFileSystemService fileSystem)
    {
        _fileSystem = fileSystem;
    }

    /// <inheritdoc />
    public Task<TemplateResult> GetTemplatesAsync(CancellationToken ct = default)
    {
        var templatesPath = GetTemplatesPath();

        if (!_fileSystem.Exists(templatesPath))
        {
            return Task.FromResult(TemplateResult.Failed("default", $"Templates directory not found: {templatesPath}"));
        }

        var templates = new List<TemplateFile>();
        var files = _fileSystem.GetFiles(templatesPath, "*", recursive: true);

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();

            var relativePath = Path.GetRelativePath(templatesPath, file)
                .Replace(Path.DirectorySeparatorChar, '/');
            var content = _fileSystem.ReadAllText(file);

            templates.Add(new TemplateFile(relativePath, content));
        }

        return Task.FromResult(new TemplateResult(templates, "default"));
    }

    /// <inheritdoc />
    public Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        var templatesPath = GetTemplatesPath();
        return Task.FromResult(_fileSystem.Exists(templatesPath));
    }

    /// <summary>
    /// Get the bundled templates directory path.
    /// </summary>
    private static string GetTemplatesPath()
    {
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? Environment.CurrentDirectory;
        return Path.Combine(assemblyDir, "templates", ".github");
    }
}
