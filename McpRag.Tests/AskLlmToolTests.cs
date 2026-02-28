using McpRag.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using McpRag;
using System.Net.Http;
using Xunit;

namespace McpRag.Tests;

public class AskLlmToolTests
{
    [Fact]
    public async Task AskLlm_ShouldReturnResponse_ForValidQuestion()
    {
        // Arrange
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<AskLlmTool>();
        var config = Options.Create(new OllamaConfig());
        var httpClient = new HttpClient();
        var ollamaLogger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<OllamaService>();
        var ollamaService = new OllamaService(httpClient, config, ollamaLogger);
        var askLlmTool = new AskLlmTool(ollamaService, config, logger);
        var testQuestion = "What is RAG?";

        // Act
        var result = await askLlmTool.AskLlm(testQuestion);

        // Assert
        Assert.False(string.IsNullOrEmpty(result));
    }
}
