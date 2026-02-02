namespace DevTools.CopilotAssets.Tests.Domain;

using DevTools.CopilotAssets.Domain;

public class VerifyResultTests
{
    [Fact]
    public void FromAssets_CalculatesSummaryCorrectly()
    {
        var assets = new List<VerifyAssetResult>
        {
            new("prompt", "file1.md", "prompts/file1.md", VerifyStatus.Valid),
            new("prompt", "file2.md", "prompts/file2.md", VerifyStatus.Valid),
            new("agent", "file3.md", "agents/file3.md", VerifyStatus.Modified),
            new("skill", "file4.md", "skills/file4.md", VerifyStatus.Missing),
            new("prompt", "file5.md", "prompts/file5.md", VerifyStatus.Restored)
        };

        var result = VerifyResult.FromAssets(assets);

        Assert.Equal(5, result.Summary.Total);
        Assert.Equal(2, result.Summary.Valid);
        Assert.Equal(1, result.Summary.Modified);
        Assert.Equal(1, result.Summary.Missing);
        Assert.Equal(1, result.Summary.Restored);
    }

    [Fact]
    public void ExitCode_WhenAllValid_ReturnsZero()
    {
        var assets = new List<VerifyAssetResult>
        {
            new("prompt", "file1.md", "prompts/file1.md", VerifyStatus.Valid),
            new("prompt", "file2.md", "prompts/file2.md", VerifyStatus.Valid),
        };

        var result = VerifyResult.FromAssets(assets);

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public void ExitCode_WhenModifiedFiles_ReturnsOne()
    {
        var assets = new List<VerifyAssetResult>
        {
            new("prompt", "file1.md", "prompts/file1.md", VerifyStatus.Valid),
            new("prompt", "file2.md", "prompts/file2.md", VerifyStatus.Modified),
        };

        var result = VerifyResult.FromAssets(assets);

        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public void ExitCode_WhenMissingFiles_ReturnsOne()
    {
        var assets = new List<VerifyAssetResult>
        {
            new("prompt", "file1.md", "prompts/file1.md", VerifyStatus.Valid),
            new("prompt", "file2.md", "prompts/file2.md", VerifyStatus.Missing),
        };

        var result = VerifyResult.FromAssets(assets);

        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public void ExitCode_WhenAllRestored_ReturnsZero()
    {
        var assets = new List<VerifyAssetResult>
        {
            new("prompt", "file1.md", "prompts/file1.md", VerifyStatus.Valid),
            new("prompt", "file2.md", "prompts/file2.md", VerifyStatus.Restored),
        };

        var result = VerifyResult.FromAssets(assets);

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public void NoManifest_ReturnsErrorWithExitCodeOne()
    {
        var result = VerifyResult.NoManifest();

        Assert.Equal(1, result.ExitCode);
        Assert.Single(result.Errors);
        Assert.Contains("manifest", result.Errors[0].ToLower());
    }

    [Fact]
    public void FromAssets_IncludesWarnings()
    {
        var assets = new List<VerifyAssetResult>
        {
            new("prompt", "file1.md", "prompts/file1.md", VerifyStatus.Modified),
        };
        var warnings = new List<string> { "file1.md has been modified locally" };

        var result = VerifyResult.FromAssets(assets, warnings: warnings);

        Assert.Single(result.Warnings);
        Assert.Equal("file1.md has been modified locally", result.Warnings[0]);
    }
}
