using DevTools.CopilotAssets.Services.Skills;
using FluentAssertions;

namespace DevTools.CopilotAssets.Tests.Services.Skills;

public class SkillParserTests
{
    [Fact]
    public void Parse_WithValidFrontmatter_ShouldReturnSkillDefinition()
    {
        var content = """
            ---
            name: refactor
            description: Refactor code to improve quality
            ---
            # Refactor Code

            Refactor code while preserving behavior.
            """;

        var result = SkillParser.Parse(content);

        result.Should().NotBeNull();
        result!.Name.Should().Be("refactor");
        result.Description.Should().Be("Refactor code to improve quality");
        result.Body.Should().Contain("Refactor code while preserving behavior.");
    }

    [Fact]
    public void Parse_WithoutFrontmatter_ShouldExtractFromHeading()
    {
        var content = """
            # Analyze File Skill

            Analyze the structure of a code file.
            """;

        var result = SkillParser.Parse(content);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Analyze File Skill");
        result.Description.Should().Be("Analyze the structure of a code file.");
    }

    [Fact]
    public void Parse_WithExtraMetadata_ShouldIncludeInMetadata()
    {
        var content = """
            ---
            name: review
            description: Code review skill
            version: 1.0
            author: Team
            ---
            # Review

            Review code.
            """;

        var result = SkillParser.Parse(content);

        result.Should().NotBeNull();
        result!.Metadata.Should().NotBeNull();
        result.Metadata!["version"].Should().Be("1.0");
        result.Metadata["author"].Should().Be("Team");
    }

    [Fact]
    public void Parse_WithEmptyContent_ShouldReturnNull()
    {
        var result = SkillParser.Parse("");

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithNullContent_ShouldReturnNull()
    {
        var result = SkillParser.Parse(null!);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithNoNameOrHeading_ShouldReturnNull()
    {
        var content = "Just some text with no heading or frontmatter name.";

        var result = SkillParser.Parse(content);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithMissingName_ShouldReturnNull()
    {
        var content = """
            ---
            description: No name field
            ---
            # Content

            Some body.
            """;

        var result = SkillParser.Parse(content);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithQuotedValues_ShouldRemoveQuotes()
    {
        var content = """
            ---
            name: "my-skill"
            description: 'A quoted description'
            ---
            # Content

            Body.
            """;

        var result = SkillParser.Parse(content);

        result.Should().NotBeNull();
        result!.Name.Should().Be("my-skill");
        result.Description.Should().Be("A quoted description");
    }

    [Fact]
    public void Validate_WithValidSkill_ShouldReturnNoErrors()
    {
        var content = """
            ---
            name: refactor
            description: Refactoring skill
            ---
            # Refactor

            Instructions for refactoring code.
            """;

        var errors = SkillParser.Validate(content, "SKILL.md");

        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyContent_ShouldReturnError()
    {
        var errors = SkillParser.Validate("", "empty.md");

        errors.Should().ContainSingle().Which.Should().Contain("empty");
    }

    [Fact]
    public void Validate_WithMissingName_ShouldReturnError()
    {
        var content = "No heading or frontmatter.";

        var errors = SkillParser.Validate(content, "bad.md");

        errors.Should().ContainSingle().Which.Should().Contain("name");
    }

    [Fact]
    public void Generate_ShouldCreateValidSkillMd()
    {
        var skill = new DevTools.CopilotAssets.Domain.SkillDefinition
        {
            Name = "test-skill",
            Description = "A test skill",
            Body = "# Test\n\nBody content."
        };

        var result = SkillParser.Generate(skill);

        result.Should().Contain("name: test-skill");
        result.Should().Contain("description: A test skill");
        result.Should().Contain("# Test");
        result.Should().Contain("Body content.");
    }
}
