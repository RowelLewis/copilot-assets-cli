namespace DevTools.CopilotAssets.Domain;

/// <summary>
/// Result of a validation operation.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>
    /// Whether the project is compliant with the policy.
    /// </summary>
    public bool IsCompliant => Errors.Count == 0;

    /// <summary>
    /// Critical issues that must be resolved.
    /// </summary>
    public List<string> Errors { get; } = [];

    /// <summary>
    /// Non-critical issues that should be reviewed.
    /// </summary>
    public List<string> Warnings { get; } = [];

    /// <summary>
    /// Informational messages.
    /// </summary>
    public List<string> Info { get; } = [];

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success() => new();

    /// <summary>
    /// Creates a failed validation result with errors.
    /// </summary>
    public static ValidationResult Failure(params string[] errors)
    {
        var result = new ValidationResult();
        result.Errors.AddRange(errors);
        return result;
    }
}
