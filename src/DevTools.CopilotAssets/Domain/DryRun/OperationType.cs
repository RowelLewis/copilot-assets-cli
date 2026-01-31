namespace DevTools.CopilotAssets.Domain;

/// <summary>
/// Type of planned operation during dry-run.
/// </summary>
public enum OperationType
{
    Create,
    Update,
    Delete,
    Skip,
    Modify
}
