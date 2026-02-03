namespace DevTools.CopilotAssets.Tests.Domain;

using DevTools.CopilotAssets.Domain;

public class AssetInfoTests
{
    [Fact]
    public void AssetInfo_RecordEquality_Works()
    {
        var asset1 = new AssetInfo("prompt", "test.md", "prompts/test.md", true, "checksum123");
        var asset2 = new AssetInfo("prompt", "test.md", "prompts/test.md", true, "checksum123");
        var asset3 = new AssetInfo("agent", "test.md", "agents/test.md", true, "checksum123");

        Assert.Equal(asset1, asset2);
        Assert.NotEqual(asset1, asset3);
    }

    [Fact]
    public void AssetInfo_WithReason_SetsProperty()
    {
        var asset = new AssetInfo("prompt", "test.md", "prompts/test.md", false, null, "modified");

        Assert.False(asset.Valid);
        Assert.Equal("modified", asset.Reason);
    }

    [Fact]
    public void AssetSummary_RecordEquality_Works()
    {
        var summary1 = new AssetSummary(10, 8, 1, 1);
        var summary2 = new AssetSummary(10, 8, 1, 1);
        var summary3 = new AssetSummary(10, 7, 2, 1);

        Assert.Equal(summary1, summary2);
        Assert.NotEqual(summary1, summary3);
    }

    [Fact]
    public void AssetListResult_ContainsAllProperties()
    {
        var assets = new List<AssetInfo>
        {
            new("prompt", "test.md", "prompts/test.md", true, "checksum123"),
        };
        var summary = new AssetSummary(1, 1, 0, 0);
        var result = new AssetListResult("/path/to/project", assets, summary);

        Assert.Equal("/path/to/project", result.ProjectPath);
        Assert.Single(result.Assets);
        Assert.Equal(1, result.Summary.Total);
    }
}
