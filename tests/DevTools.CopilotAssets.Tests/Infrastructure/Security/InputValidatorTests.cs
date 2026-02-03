using DevTools.CopilotAssets.Infrastructure.Security;
using FluentAssertions;

namespace DevTools.CopilotAssets.Tests.Infrastructure.Security;

public class InputValidatorTests
{
    [Theory]
    [InlineData("valid/repo")]
    [InlineData("my-org/my-repo")]
    [InlineData("test123/test456")]
    [InlineData("user_name/repo-name")]
    public void IsValidRepository_WithValidFormat_ShouldReturnTrue(string source)
    {
        // Act
        var result = InputValidator.IsValidRepository(source);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("invalid/repo/extra")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("/repo")]
    [InlineData("owner/")]
    [InlineData("../etc/passwd")]
    [InlineData("owner\\repo")]
    [InlineData("owner/repo/../../../../etc/passwd")]
    public void IsValidRepository_WithInvalidFormat_ShouldReturnFalse(string? source)
    {
        // Act
        var result = InputValidator.IsValidRepository(source);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("main")]
    [InlineData("develop")]
    [InlineData("feature-branch")]
    [InlineData("v1.0.0")]
    public void IsValidBranch_WithValidBranch_ShouldReturnTrue(string branch)
    {
        // Act
        var result = InputValidator.IsValidBranch(branch);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("../main")]
    [InlineData("feature/branch")]
    [InlineData("refs/heads/main")]
    [InlineData("HEAD")]
    [InlineData("main\\branch")]
    public void IsValidBranch_WithInvalidBranch_ShouldReturnFalse(string? branch)
    {
        // Act
        var result = InputValidator.IsValidBranch(branch);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("../etc/passwd")]
    [InlineData("..\\windows\\system32")]
    [InlineData("/etc/passwd")]
    [InlineData("some\\path")]
    [InlineData("")]
    public void SanitizePath_WithInvalidPath_ShouldThrowSecurityException(string path)
    {
        // Act
        var act = () => InputValidator.SanitizePath(path);

        // Assert
        act.Should().Throw<SecurityException>();
    }

    [Fact]
    public void SanitizePath_WithNull_ShouldThrowSecurityException()
    {
        // Act
        var act = () => InputValidator.SanitizePath(null!);

        // Assert
        act.Should().Throw<SecurityException>();
    }

    [Theory]
    [InlineData("copilot-instructions.md", "copilot-instructions.md")]
    [InlineData("prompts/test.md", "prompts/test.md")]
    [InlineData("/leading/slash.md", "leading/slash.md")]
    public void SanitizePath_WithValidPath_ShouldReturnSanitized(string input, string expected)
    {
        // Act
        var result = InputValidator.SanitizePath(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(".github/copilot-instructions.md")]
    [InlineData(".github/prompts/test.md")]
    [InlineData(".github")]
    public void IsWithinGitHubDirectory_WithValidPath_ShouldReturnTrue(string path)
    {
        // Act
        var result = InputValidator.IsWithinGitHubDirectory(path);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("etc/passwd")]
    [InlineData("../etc/passwd")]
    [InlineData("some/other/path")]
    public void IsWithinGitHubDirectory_WithInvalidPath_ShouldReturnFalse(string path)
    {
        // Act
        var result = InputValidator.IsWithinGitHubDirectory(path);

        // Assert
        result.Should().BeFalse();
    }
}
