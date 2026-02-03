using DevTools.CopilotAssets.Services.Templates;

namespace DevTools.CopilotAssets.Tests.Services.Templates;

public class TemplateFileTests
{
    [Fact]
    public void GetTargetPath_ShouldCombinePaths()
    {
        // Arrange
        var template = new TemplateFile("agents/test.agent.md", "content");
        var targetDir = "/tmp/test/.github";

        // Act
        var result = template.GetTargetPath(targetDir);

        // Assert
        result.Should().Contain("agents");
        result.Should().EndWith("test.agent.md");
        result.Should().StartWith(targetDir);
    }

    [Fact]
    public void GetTargetPath_ShouldHandleNestedPaths()
    {
        // Arrange
        var template = new TemplateFile("prompts/subdir/nested.prompt.md", "content");
        var targetDir = "/project/.github";

        // Act
        var result = template.GetTargetPath(targetDir);

        // Assert
        result.Should().Contain("prompts");
        result.Should().Contain("subdir");
        result.Should().EndWith("nested.prompt.md");
    }

    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        // Act
        var template = new TemplateFile("test.md", "test content");

        // Assert
        template.RelativePath.Should().Be("test.md");
        template.Content.Should().Be("test content");
    }
}
