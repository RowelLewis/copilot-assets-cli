using System.Text.Json.Serialization;

namespace DevTools.CopilotAssets.Domain;

/// <summary>
/// Result of a dry-run operation.
/// </summary>
public sealed class DryRunResult
{
    [JsonPropertyName("dryRun")]
    public bool IsDryRun => true;

    [JsonPropertyName("operations")]
    public IReadOnlyList<PlannedOperation> Operations { get; init; } = [];

    [JsonPropertyName("summary")]
    public DryRunSummary Summary { get; init; } = new(0, 0, 0, 0, 0);

    [JsonIgnore]
    public int ExitCode => Summary.Creates > 0 || Summary.Updates > 0 || Summary.Deletes > 0 ? 2 : 0;

    public static DryRunResult FromOperations(IReadOnlyList<PlannedOperation> operations)
    {
        var summary = new DryRunSummary(
            Creates: operations.Count(o => o.Type == OperationType.Create),
            Updates: operations.Count(o => o.Type == OperationType.Update),
            Deletes: operations.Count(o => o.Type == OperationType.Delete),
            Skips: operations.Count(o => o.Type == OperationType.Skip),
            Modifies: operations.Count(o => o.Type == OperationType.Modify));

        return new DryRunResult
        {
            Operations = operations,
            Summary = summary
        };
    }
}
