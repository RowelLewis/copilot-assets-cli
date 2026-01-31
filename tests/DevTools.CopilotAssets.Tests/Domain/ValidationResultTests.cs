using DevTools.CopilotAssets.Domain;

namespace DevTools.CopilotAssets.Tests.Domain;

public class ValidationResultTests
{
    [Fact]
    public void NewValidationResult_ShouldBeCompliant()
    {
        // Arrange & Act
        var result = new ValidationResult();

        // Assert
        result.IsCompliant.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
        result.Info.Should().BeEmpty();
    }

    [Fact]
    public void Success_ShouldReturnCompliantResult()
    {
        // Act
        var result = ValidationResult.Success();

        // Assert
        result.IsCompliant.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Failure_WithSingleError_ShouldReturnNonCompliantResult()
    {
        // Act
        var result = ValidationResult.Failure("Something went wrong");

        // Assert
        result.IsCompliant.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors.Should().Contain("Something went wrong");
    }

    [Fact]
    public void Failure_WithMultipleErrors_ShouldReturnNonCompliantResult()
    {
        // Act
        var result = ValidationResult.Failure("Error 1", "Error 2", "Error 3");

        // Assert
        result.IsCompliant.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
        result.Errors.Should().Contain("Error 1");
        result.Errors.Should().Contain("Error 2");
        result.Errors.Should().Contain("Error 3");
    }

    [Fact]
    public void IsCompliant_ShouldBeFalse_WhenErrorsExist()
    {
        // Arrange
        var result = new ValidationResult();
        result.Errors.Add("An error occurred");

        // Assert
        result.IsCompliant.Should().BeFalse();
    }

    [Fact]
    public void IsCompliant_ShouldBeTrue_WhenOnlyWarningsExist()
    {
        // Arrange
        var result = new ValidationResult();
        result.Warnings.Add("A warning");

        // Assert
        result.IsCompliant.Should().BeTrue();
    }

    [Fact]
    public void IsCompliant_ShouldBeTrue_WhenOnlyInfoExists()
    {
        // Arrange
        var result = new ValidationResult();
        result.Info.Add("Some info");

        // Assert
        result.IsCompliant.Should().BeTrue();
    }

    [Fact]
    public void CanAddMultipleMessages()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        result.Errors.Add("Error 1");
        result.Errors.Add("Error 2");
        result.Warnings.Add("Warning 1");
        result.Info.Add("Info 1");
        result.Info.Add("Info 2");
        result.Info.Add("Info 3");

        // Assert
        result.Errors.Should().HaveCount(2);
        result.Warnings.Should().HaveCount(1);
        result.Info.Should().HaveCount(3);
        result.IsCompliant.Should().BeFalse();
    }
}
