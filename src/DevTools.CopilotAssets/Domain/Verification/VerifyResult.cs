using System.Text.Json.Serialization;

namespace DevTools.CopilotAssets.Domain;

/// <summary>
/// Result of a verify operation.
/// </summary>
public sealed class VerifyResult
{
    [JsonPropertyName("assets")]
    public IReadOnlyList<VerifyAssetResult> Assets { get; init; } = [];

    [JsonPropertyName("summary")]
    public VerifySummary Summary { get; init; } = new(0, 0, 0, 0, 0);

    [JsonIgnore]
    public int ExitCode { get; init; }

    [JsonIgnore]
    public IReadOnlyList<string> Errors { get; init; } = [];

    [JsonIgnore]
    public IReadOnlyList<string> Warnings { get; init; } = [];

    public static VerifyResult FromAssets(
        IReadOnlyList<VerifyAssetResult> assets,
        IReadOnlyList<string>? errors = null,
        IReadOnlyList<string>? warnings = null)
    {
        var valid = assets.Count(a => a.Status == VerifyStatus.Valid);
        var modified = assets.Count(a => a.Status == VerifyStatus.Modified);
        var missing = assets.Count(a => a.Status == VerifyStatus.Missing);
        var restored = assets.Count(a => a.Status == VerifyStatus.Restored);

        var hasInvalid = modified > 0 || missing > 0;

        return new VerifyResult
        {
            Assets = assets,
            Summary = new VerifySummary(assets.Count, valid, modified, missing, restored),
            ExitCode = hasInvalid ? 1 : 0,
            Errors = errors ?? [],
            Warnings = warnings ?? []
        };
    }

    public static VerifyResult NoManifest() => new()
    {
        Assets = [],
        Summary = new VerifySummary(0, 0, 0, 0, 0),
        ExitCode = 1,
        Errors = ["No manifest found. Run 'copilot-assets init' first."],
        Warnings = []
    };
}
