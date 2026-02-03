namespace DevTools.CopilotAssets.Tests.Domain;

using DevTools.CopilotAssets.Domain;

public class DryRunResultTests
{
    [Fact]
    public void FromOperations_CalculatesSummaryCorrectly()
    {
        var operations = new List<PlannedOperation>
        {
            new(OperationType.Create, "file1.md"),
            new(OperationType.Create, "file2.md"),
            new(OperationType.Update, "file3.md", "content differs"),
            new(OperationType.Skip, "file4.md", "exists"),
            new(OperationType.Modify, ".gitignore", "append entry")
        };

        var result = DryRunResult.FromOperations(operations);

        Assert.Equal(2, result.Summary.Creates);
        Assert.Equal(1, result.Summary.Updates);
        Assert.Equal(0, result.Summary.Deletes);
        Assert.Equal(1, result.Summary.Skips);
        Assert.Equal(1, result.Summary.Modifies);
    }

    [Fact]
    public void ExitCode_WhenChangesWouldBeMade_ReturnsTwo()
    {
        var operations = new List<PlannedOperation>
        {
            new(OperationType.Create, "file1.md"),
        };

        var result = DryRunResult.FromOperations(operations);

        Assert.Equal(2, result.ExitCode);
    }

    [Fact]
    public void ExitCode_WhenNoChanges_ReturnsZero()
    {
        var operations = new List<PlannedOperation>
        {
            new(OperationType.Skip, "file1.md", "unchanged"),
        };

        var result = DryRunResult.FromOperations(operations);

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public void IsDryRun_AlwaysTrue()
    {
        var result = DryRunResult.FromOperations([]);
        Assert.True(result.IsDryRun);
    }

    [Fact]
    public void EmptyOperations_ReturnsSummaryWithZeros()
    {
        var result = DryRunResult.FromOperations([]);

        Assert.Equal(0, result.Summary.Creates);
        Assert.Equal(0, result.Summary.Updates);
        Assert.Equal(0, result.Summary.Deletes);
        Assert.Equal(0, result.Summary.Skips);
        Assert.Equal(0, result.Summary.Modifies);
    }
}
