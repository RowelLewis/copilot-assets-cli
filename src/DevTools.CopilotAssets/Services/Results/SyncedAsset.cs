namespace DevTools.CopilotAssets.Services;

/// <summary>
/// Information about a synced asset.
/// </summary>
public sealed class SyncedAsset
{
    public required string RelativePath { get; init; }
    public required string FullPath { get; init; }
    public required string Checksum { get; init; }
    public bool WasUpdated { get; init; }
}
