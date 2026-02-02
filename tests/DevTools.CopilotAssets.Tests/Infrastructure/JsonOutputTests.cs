namespace DevTools.CopilotAssets.Tests.Infrastructure;

using DevTools.CopilotAssets.Infrastructure;

public class JsonOutputTests
{
    [Fact]
    public void Serialize_ProducesValidJson()
    {
        var result = JsonOutput.CreateResult("test", new { value = 42 });
        var json = JsonOutput.Serialize(result);

        // Should parse without throwing
        var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.NotNull(doc);
    }

    [Fact]
    public void Serialize_UsesCamelCase()
    {
        var result = JsonOutput.CreateResult("test", new { MyProperty = "value" });
        var json = JsonOutput.Serialize(result);

        Assert.Contains("myProperty", json);
        Assert.DoesNotContain("MyProperty", json);
    }

    [Fact]
    public void CreateResult_IncludesCommand()
    {
        var result = JsonOutput.CreateResult("test-command", new { });

        Assert.Equal("test-command", result.Command);
    }

    [Fact]
    public void CreateResult_IncludesTimestamp()
    {
        var before = DateTime.UtcNow;
        var result = JsonOutput.CreateResult("test", new { });
        var after = DateTime.UtcNow;

        Assert.InRange(result.Timestamp, before, after);
    }

    [Fact]
    public void CreateResult_SuccessTrue_WhenExitCodeZero()
    {
        var result = JsonOutput.CreateResult("test", new { }, exitCode: 0);

        Assert.True(result.Success);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public void CreateResult_SuccessFalse_WhenExitCodeNonZero()
    {
        var result = JsonOutput.CreateResult("test", new { }, exitCode: 1);

        Assert.False(result.Success);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public void CreateResult_IncludesErrors()
    {
        var errors = new List<string> { "error1", "error2" };
        var result = JsonOutput.CreateResult("test", new { }, exitCode: 1, errors: errors);

        Assert.Equal(2, result.Errors.Count);
        Assert.Contains("error1", result.Errors);
        Assert.Contains("error2", result.Errors);
    }

    [Fact]
    public void CreateResult_IncludesWarnings()
    {
        var warnings = new List<string> { "warning1" };
        var result = JsonOutput.CreateResult("test", new { }, warnings: warnings);

        Assert.Single(result.Warnings);
        Assert.Contains("warning1", result.Warnings);
    }

    [Fact]
    public void CreateResult_EmptyCollections_WhenNull()
    {
        var result = JsonOutput.CreateResult("test", new { });

        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Serialize_IncludesAllFields()
    {
        var result = JsonOutput.CreateResult("test", new { data = "value" });
        var json = JsonOutput.Serialize(result);

        Assert.Contains("\"command\"", json);
        Assert.Contains("\"version\"", json);
        Assert.Contains("\"timestamp\"", json);
        Assert.Contains("\"success\"", json);
        Assert.Contains("\"exitCode\"", json);
        Assert.Contains("\"result\"", json);
        Assert.Contains("\"errors\"", json);
        Assert.Contains("\"warnings\"", json);
    }

    [Fact]
    public void Serialize_IsIndented()
    {
        var result = JsonOutput.CreateResult("test", new { });
        var json = JsonOutput.Serialize(result);

        // Indented JSON contains newlines
        Assert.Contains("\n", json);
    }
}
