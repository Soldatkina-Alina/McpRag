using McpRag.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using McpRag;
using System.Net.Http;
using Xunit;

namespace McpRag.Tests;

public class CheckOllamaToolTests
{
    [Fact]
    public async Task CheckOllama_ShouldReturnStatus_OfOllama()
    {
        // Arrange
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<CheckOllamaTool>();
        var config = Options.Create(new OllamaConfig());
        var httpClient = new HttpClient();
        var ollamaLogger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<OllamaService>();
        var ollamaService = new OllamaService(httpClient, config, ollamaLogger);
        var checkOllamaTool = new CheckOllamaTool(ollamaService, config, logger);

        // Act
        var result = await checkOllamaTool.CheckOllama();

        // Assert
        Assert.False(string.IsNullOrEmpty(result));
    }
}
