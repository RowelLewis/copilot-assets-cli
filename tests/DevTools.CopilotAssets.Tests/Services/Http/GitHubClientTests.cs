using System.Net;
using DevTools.CopilotAssets.Services.Http;
using FluentAssertions;
using Xunit;

namespace DevTools.CopilotAssets.Tests.Services.Http;

public class GitHubClientTests
{
    [Fact]
    public void Constructor_ShouldAcceptCustomHttpClient()
    {
        // Arrange
        var customClient = new HttpClient();

        // Act
        using var gitHubClient = new GitHubClient(customClient);

        // Assert
        gitHubClient.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldCreateDefaultClient_WhenNoClientProvided()
    {
        // Act
        using var gitHubClient = new GitHubClient();

        // Assert
        gitHubClient.Should().NotBeNull();
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var gitHubClient = new GitHubClient();

        // Act
        var act = () => gitHubClient.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task GetDirectoryContentsAsync_ShouldHandleHttpErrors()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(HttpStatusCode.NotFound, "Not Found");
        var httpClient = new HttpClient(handler);
        using var gitHubClient = new GitHubClient(httpClient);

        // Act
        var result = await gitHubClient.GetDirectoryContentsAsync("test", "repo", ".github", "main");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("NotFound");
    }

    [Fact]
    public async Task DownloadFileAsync_ShouldHandleHttpErrors()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(HttpStatusCode.NotFound, "Not Found");
        var httpClient = new HttpClient(handler);
        using var gitHubClient = new GitHubClient(httpClient);

        // Act
        var result = await gitHubClient.DownloadFileAsync("https://raw.githubusercontent.com/test/repo/main/.github/test.md");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DownloadFileAsync_ShouldReturnContent_OnSuccess()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, "file content");
        var httpClient = new HttpClient(handler);
        using var gitHubClient = new GitHubClient(httpClient);

        // Act
        var result = await gitHubClient.DownloadFileAsync("https://raw.githubusercontent.com/test/repo/main/.github/test.md");

        // Assert
        result.Should().Be("file content");
    }
}

/// <summary>
/// Test HTTP message handler for mocking HTTP responses
/// </summary>
public class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _content;

    public TestHttpMessageHandler(HttpStatusCode statusCode, string content)
    {
        _statusCode = statusCode;
        _content = content;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_content)
        };

        return Task.FromResult(response);
    }
}
