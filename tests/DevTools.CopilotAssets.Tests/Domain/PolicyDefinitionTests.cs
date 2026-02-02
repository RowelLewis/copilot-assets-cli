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
            RequiredFiles = ["copilot-instructions.md"]
        };

        // Assert
        policy.RequiredFiles.Should().HaveCount(1);
        policy.RestrictedPatterns.Should().BeEmpty();
        policy.EnforceInCi.Should().BeTrue();
    }

    [Fact]
    public void PolicyDefinition_ShouldAcceptCustomValues()
    {
        // Arrange & Act
        var policy = new PolicyDefinition
        {
            RequiredFiles = ["copilot-instructions.md", "prompts/review.md"],
            RestrictedPatterns = [@"api[_-]?key", @"password\s*="],
            EnforceInCi = false
        };

        // Assert
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
            RequiredFiles = ["copilot-instructions.md"],
            EnforceInCi = true
        };

        var policy2 = new PolicyDefinition
        {
            RequiredFiles = ["copilot-instructions.md"],
            EnforceInCi = true
        };

        // Assert - use BeEquivalentTo for deep comparison
        policy1.Should().BeEquivalentTo(policy2);
    }
}
