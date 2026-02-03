using DevTools.CopilotAssets.Infrastructure.Security;
using FluentAssertions;

namespace DevTools.CopilotAssets.Tests.Infrastructure.Security;

public class TokenRedactorTests
{
    [Theory]
    [InlineData("ghp_1234567890abcdefghijklmnopqrstuvwxyz1234", "[REDACTED_TOKEN]")]
    [InlineData("gho_1234567890abcdefghijklmnopqrstuvwxyz1234", "[REDACTED_TOKEN]")]
    [InlineData("Bearer abc123def456ghi789", "Bearer [REDACTED]")]
    [InlineData("token=abc123def456", "token=[REDACTED]")]
    [InlineData("API_KEY=1234567890abcdef", "API_KEY=[REDACTED]")]
    public void RedactSensitiveData_ShouldMaskTokens(string input, string expectedSubstring)
    {
        // Act
        var result = TokenRedactor.RedactSensitiveData(input);

        // Assert
        result.Should().Contain(expectedSubstring);
        result.Should().NotContain(input.Contains("=") ? input.Split('=')[1] : input.Split(' ').Last());
    }

    [Fact]
    public void RedactSensitiveData_WithMultipleTokens_ShouldMaskAll()
    {
        // Arrange
        var input = "ghp_token123 and Bearer xyz789 and api_key=secret123";

        // Act
        var result = TokenRedactor.RedactSensitiveData(input);

        // Assert
        result.Should().Contain("[REDACTED");
        result.Should().NotContain("ghp_token123");
        result.Should().NotContain("xyz789");
        result.Should().NotContain("secret123");
    }

    [Fact]
    public void RedactPaths_ShouldReplaceHomeDirectory()
    {
        // Arrange
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var input = $"Error in {homeDir}/some/path";

        // Act
        var result = TokenRedactor.RedactPaths(input);

        // Assert
        result.Should().Contain("~/some/path");
        result.Should().NotContain(homeDir);
    }

    [Fact]
    public void SanitizeException_ShouldRedactTokensAndPaths()
    {
        // Arrange
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var ex = new Exception($"Failed at {homeDir}/file.txt with token ghp_abc123def456ghi789");

        // Act
        var result = TokenRedactor.SanitizeException(ex);

        // Assert
        result.Should().Contain("~/file.txt");
        result.Should().Contain("[REDACTED_TOKEN]");
        result.Should().NotContain("ghp_abc123def456ghi789");
        result.Should().NotContain(homeDir);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void RedactSensitiveData_WithNullOrEmpty_ShouldReturnAsIs(string? input)
    {
        // Act
        var result = TokenRedactor.RedactSensitiveData(input!);

        // Assert
        result.Should().Be(input);
    }

    [Fact]
    public void RedactSensitiveData_WithNoSensitiveData_ShouldReturnUnchanged()
    {
        // Arrange
        var input = "This is a normal message with no secrets";

        // Act
        var result = TokenRedactor.RedactSensitiveData(input);

        // Assert
        result.Should().Be(input);
    }
}
