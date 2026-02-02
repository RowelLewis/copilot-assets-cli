using DevTools.CopilotAssets.Services.Templates;

namespace DevTools.CopilotAssets.Tests.Services.Templates;

public class TemplateResultTests
{
    [Fact]
    public void Empty_ShouldCreateEmptyResult()
    {
        // Act
        var result = TemplateResult.Empty("test-source");

        // Assert
        result.Templates.Should().BeEmpty();
        result.Source.Should().Be("test-source");
        result.FromCache.Should().BeFalse();
        result.HasTemplates.Should().BeFalse();
        result.HasError.Should().BeFalse();
    }

    [Fact]
    public void Failed_ShouldCreateResultWithError()
    {
        // Act
        var result = TemplateResult.Failed("test-source", "Network error");

        // Assert
        result.Templates.Should().BeEmpty();
        result.Source.Should().Be("test-source");
        result.Error.Should().Be("Network error");
        result.HasError.Should().BeTrue();
        result.HasTemplates.Should().BeFalse();
    }

    [Fact]
    public void HasTemplates_ShouldReturnTrue_WhenTemplatesExist()
    {
        // Arrange
        var templates = new List<TemplateFile>
        {
            new("test.md", "content")
        };

        // Act
        var result = new TemplateResult(templates, "source", false);

        // Assert
        result.HasTemplates.Should().BeTrue();
    }

    [Fact]
    public void HasError_ShouldReturnTrue_WhenErrorIsSet()
    {
        // Arrange
        var result = new TemplateResult([], "source", false)
        {
            Error = "Something went wrong"
        };

        // Assert
        result.HasError.Should().BeTrue();
    }
}
