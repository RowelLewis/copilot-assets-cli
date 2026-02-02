using System.Text.Json.Serialization;

namespace DevTools.CopilotAssets.Domain;

/// <summary>
/// Tracks the source of templates (default or remote).
/// </summary>
public sealed class TemplateSource
{
    /// <summary>
    /// Source type: default or remote.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    /// <summary>
    /// Repository in owner/repo format (for remote).
    /// </summary>
    [JsonPropertyName("repo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Repo { get; set; }

    /// <summary>
    /// Branch name (for remote).
    /// </summary>
    [JsonPropertyName("branch")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Branch { get; set; }

    /// <summary>
    /// Create a default (built-in) source.
    /// </summary>
    public static TemplateSource Default() => new() { Type = "default" };

    /// <summary>
    /// Create a remote source.
    /// </summary>
    public static TemplateSource Remote(string repo, string branch) => new()
    {
        Type = "remote",
        Repo = repo,
        Branch = branch
    };

    /// <summary>
    /// Parse source from a source string like "remote:owner/repo@branch".
    /// </summary>
    public static TemplateSource Parse(string source)
    {
        if (string.IsNullOrEmpty(source) || source == "default" || source == "bundled")
        {
            return Default();
        }

        if (source.StartsWith("remote:"))
        {
            var rest = source.Substring(7); // "remote:".Length

            var atIndex = rest.IndexOf('@');
            if (atIndex > 0)
            {
                return new TemplateSource
                {
                    Type = "remote",
                    Repo = rest.Substring(0, atIndex),
                    Branch = rest.Substring(atIndex + 1)
                };
            }

            return new TemplateSource { Type = "remote", Repo = rest };
        }

        return Default();
    }

    /// <summary>
    /// Format as display string.
    /// </summary>
    public override string ToString()
    {
        if (Type == "default")
            return "default templates";

        if (!string.IsNullOrEmpty(Repo) && !string.IsNullOrEmpty(Branch))
            return $"remote: {Repo}@{Branch}";

        if (!string.IsNullOrEmpty(Repo))
            return $"remote: {Repo}";

        return Type;
    }
}
