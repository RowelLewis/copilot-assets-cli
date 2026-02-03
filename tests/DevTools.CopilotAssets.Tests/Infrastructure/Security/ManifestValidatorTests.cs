using DevTools.CopilotAssets.Domain;
using DevTools.CopilotAssets.Infrastructure.Security;
using FluentAssertions;

namespace DevTools.CopilotAssets.Tests.Infrastructure.Security;

public class ManifestValidatorTests
{
    [Fact]
    public void Validate_WithValidManifest_ShouldNotThrow()
    {
        // Arrange
        var manifest = new Manifest
        {
            InstalledAt = DateTime.UtcNow,
            ToolVersion = "1.3.0",
            Assets = [".github/copilot-instructions.md", ".github/prompts/test.md"],
            Checksums = new Dictionary<string, string>
            {
                [".github/copilot-instructions.md"] = "abcd1234".PadRight(64, '0'),
                [".github/prompts/test.md"] = "ef563789".PadRight(64, '0')
            }
        };

        // Act
        var act = () => ManifestValidator.Validate(manifest);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithTooManyAssets_ShouldThrowSecurityException()
    {
        // Arrange
        var manifest = new Manifest
        {
            InstalledAt = DateTime.UtcNow,
            ToolVersion = "1.3.0",
            Assets = Enumerable.Range(1, 101).Select(i => $".github/asset{i}.md").ToList()
        };

        // Act
        var act = () => ManifestValidator.Validate(manifest);

        // Assert
        act.Should().Throw<SecurityException>()
            .WithMessage("*Too many assets*");
    }

    [Theory]
    [InlineData("../etc/passwd")]
    [InlineData("..\\windows\\system32")]
    [InlineData("/etc/passwd")]
    public void Validate_WithPathTraversal_ShouldThrowSecurityException(string maliciousPath)
    {
        // Arrange
        var manifest = new Manifest
        {
            InstalledAt = DateTime.UtcNow,
            ToolVersion = "1.3.0",
            Assets = [maliciousPath]
        };

        // Act
        var act = () => ManifestValidator.Validate(manifest);

        // Assert
        act.Should().Throw<SecurityException>();
    }

    [Fact]
    public void Validate_WithPathOutsideGitHub_ShouldThrowSecurityException()
    {
        // Arrange
        var manifest = new Manifest
        {
            InstalledAt = DateTime.UtcNow,
            ToolVersion = "1.3.0",
            Assets = ["etc/passwd"]
        };

        // Act
        var act = () => ManifestValidator.Validate(manifest);

        // Assert
        act.Should().Throw<SecurityException>()
            .WithMessage("*within .github directory*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("not-hex-string-abcdefgh")]
    [InlineData("1234567890abcdef")]  // Only 16 chars, not 64
    public void Validate_WithInvalidChecksum_ShouldThrowSecurityException(string invalidChecksum)
    {
        // Arrange
        var manifest = new Manifest
        {
            InstalledAt = DateTime.UtcNow,
            ToolVersion = "1.3.0",
            Assets = [".github/test.md"],
            Checksums = new Dictionary<string, string>
            {
                [".github/test.md"] = invalidChecksum
            }
        };

        // Act
        var act = () => ManifestValidator.Validate(manifest);

        // Assert
        act.Should().Throw<SecurityException>()
            .WithMessage("*checksum*");
    }

    [Fact]
    public void Validate_WithValidChecksum_ShouldNotThrow()
    {
        // Arrange
        var validChecksum = "a1b2c3d4e5f6789012345678901234567890123456789012345678901234abcd";
        var manifest = new Manifest
        {
            InstalledAt = DateTime.UtcNow,
            ToolVersion = "1.3.0",
            Assets = [".github/test.md"],
            Checksums = new Dictionary<string, string>
            {
                [".github/test.md"] = validChecksum
            }
        };

        // Act
        var act = () => ManifestValidator.Validate(manifest);

        // Assert
        act.Should().NotThrow();
    }
}
