using DevTools.CopilotAssets.Infrastructure.Security;

namespace DevTools.CopilotAssets.Tests.Infrastructure.Security;

public class AssetPathResolverTests
{
    [Theory]
    [InlineData("copilot-instructions.md", ".github/copilot-instructions.md")]
    [InlineData("prompts/test.md", ".github/prompts/test.md")]
    [InlineData("agents/my-agent.md", ".github/agents/my-agent.md")]
    public void ResolveToFileSystemPath_CopilotOnlyPath_ReturnsDotGitHubPrefixed(string trackingPath, string expected)
    {
        var result = AssetPathResolver.ResolveToFileSystemPath(trackingPath);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("claude:CLAUDE.md", "CLAUDE.md")]
    [InlineData("cursor:.cursor/rules/instructions.mdc", ".cursor/rules/instructions.mdc")]
    [InlineData("windsurf:.windsurfrules", ".windsurfrules")]
    [InlineData("cline:.clinerules/instructions.md", ".clinerules/instructions.md")]
    [InlineData("aider:.aider.instructions.md", ".aider.instructions.md")]
    public void ResolveToFileSystemPath_MultiTargetPath_ReturnsPathAfterColon(string trackingPath, string expected)
    {
        var result = AssetPathResolver.ResolveToFileSystemPath(trackingPath);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("claude:CLAUDE.md", true)]
    [InlineData("cursor:.cursor/rules/instructions.mdc", true)]
    [InlineData("windsurf:.windsurfrules", true)]
    [InlineData("CLAUDE:CLAUDE.md", true)] // case-insensitive
    [InlineData("copilot-instructions.md", false)]
    [InlineData("prompts/test.md", false)]
    [InlineData(":no-prefix.md", false)] // colon at position 0
    [InlineData("unknown:file.md", false)] // unknown prefix
    public void IsMultiTargetPath_VariousPaths_ReturnsExpected(string path, bool expected)
    {
        var result = AssetPathResolver.IsMultiTargetPath(path);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("copilot:CLAUDE.md", "CLAUDE.md")] // "copilot" is also a known target prefix
    public void ResolveToFileSystemPath_CopilotPrefixedMultiTarget_ReturnsPathAfterColon(string trackingPath, string expected)
    {
        // "copilot" prefix in a multi-target tracking path (e.g., "copilot:copilot-instructions.md")
        var result = AssetPathResolver.ResolveToFileSystemPath(trackingPath);
        result.Should().Be(expected);
    }
}
