using DevTools.CopilotAssets.Domain;

namespace DevTools.CopilotAssets.Tests.Domain;

public class PolicyDefinitionTests
{
    [Fact]
    public void PolicyDefinition_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var policy = new PolicyDefinition
        {
            MinimumVersion = "1.0.0"
        };

        // Assert
        policy.MinimumVersion.Should().Be("1.0.0");
        policy.RequiredFiles.Should().BeEmpty();
        policy.RestrictedPatterns.Should().BeEmpty();
        policy.EnforceInCi.Should().BeTrue();
    }

    [Fact]
    public void PolicyDefinition_ShouldAcceptCustomValues()
    {
        // Arrange & Act
        var policy = new PolicyDefinition
        {
            MinimumVersion = "2.0.0",
            RequiredFiles = ["copilot-instructions.md", "prompts/review.md"],
            RestrictedPatterns = [@"api[_-]?key", @"password\s*="],
            EnforceInCi = false
        };

        // Assert
        policy.MinimumVersion.Should().Be("2.0.0");
        policy.RequiredFiles.Should().HaveCount(2);
        policy.RequiredFiles.Should().Contain("copilot-instructions.md");
        policy.RestrictedPatterns.Should().HaveCount(2);
        policy.EnforceInCi.Should().BeFalse();
    }

    [Fact]
    public void PolicyDefinition_RecordEquality_ShouldWorkCorrectly()
    {
        // Arrange
        var policy1 = new PolicyDefinition
        {
            MinimumVersion = "1.0.0",
            EnforceInCi = true
        };

        var policy2 = new PolicyDefinition
        {
            MinimumVersion = "1.0.0",
            EnforceInCi = true
        };

        // Assert - use BeEquivalentTo for deep comparison
        policy1.Should().BeEquivalentTo(policy2);
    }
}
