namespace DevTools.CopilotAssets.Services;

/// <summary>
/// Result of a sync operation.
/// </summary>
public sealed class SyncResult
{
    public List<SyncedAsset> Synced { get; } = [];
    public List<string> Unchanged { get; } = [];
    public List<string> Skipped { get; } = [];
    public List<string> Errors { get; } = [];
    public List<string> Warnings { get; } = [];

    public bool Success => Errors.Count == 0;
    public int TotalProcessed => Synced.Count + Unchanged.Count + Skipped.Count;
}
