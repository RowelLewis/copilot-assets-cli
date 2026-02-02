namespace DevTools.CopilotAssets.Domain;

/// <summary>
/// Verification status of an asset.
/// </summary>
public enum VerifyStatus
{
    Valid,
    Modified,
    Missing,
    Restored
}
