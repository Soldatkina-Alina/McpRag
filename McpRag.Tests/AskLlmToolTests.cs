using McpRag;
using McpRag.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace McpRag.Tests;

/// <summary>
/// Тесты для AskLlmTool - инструмента для запросов к языковой модели Ollama.
/// Проверяют работу с запросами и обработку ошибок.
/// </summary>
public class AskLlmToolTests
{
    private readonly Mock<IOllamaService> _ollamaMock;
    private readonly AskLlmTool _askLlmTool;

    public AskLlmToolTests()
    {
        _ollamaMock = new Mock<IOllamaService>();
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<AskLlmTool>();
        var config = Options.Create(new OllamaConfig { Model = "phi3:mini", EmbeddingModel = "nomic-embed-text", BaseUrl = "http://localhost:11434" });
        _askLlmTool = new AskLlmTool(_ollamaMock.Object, config, logger);
    }

    /// <summary>
    /// Проверяет, что метод AskLlm возвращает ответ при отправке valid question.
    /// Убеждается, что ответ не пустой и содержит ожидаемую информацию.
    /// </summary>
    [Fact]
    public async Task AskLlm_ShouldReturnResponse_ForValidQuestion()
    {
        // Arrange
        var question = "What is RAG?";
        var response = "RAG stands for Retrieval-Augmented Generation. It is a technique that combines retrieval of information from external sources with text generation to produce more accurate and contextually relevant answers.";
        _ollamaMock.Setup(x => x.IsHealthyAsync()).ReturnsAsync(true);
        _ollamaMock.Setup(x => x.IsModelAvailableAsync(It.IsAny<string>())).ReturnsAsync(true);
        _ollamaMock.Setup(x => x.GenerateAsync(question)).ReturnsAsync(response);

        // Act
        var result = await _askLlmTool.AskLlm(question);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("RAG", result);
    }

    /// <summary>
    /// Проверяет, что метод AskLlm обрабатывает пустой вопрос корректно.
    /// Убеждается, что инструмент не выбрасывает исключение и возвращает сообщение о пустом вопросе.
    /// </summary>
    [Fact]
    public async Task AskLlm_WithEmptyQuestion_ShouldReturnErrorMessage()
    {
        // Arrange
        string question = string.Empty;
        _ollamaMock.Setup(x => x.IsHealthyAsync()).ReturnsAsync(true);
        _ollamaMock.Setup(x => x.IsModelAvailableAsync(It.IsAny<string>())).ReturnsAsync(true);
        _ollamaMock.Setup(x => x.GenerateAsync(question)).ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _askLlmTool.AskLlm(question);

        // Assert
        Assert.Contains("❌", result);
    }

    /// <summary>
    /// Проверяет, что метод AskLlm обрабатывает null question.
    /// Убеждается, что инструмент не выбрасывает исключение и возвращает сообщение о пустом вопросе.
    /// </summary>
    [Fact]
    public async Task AskLlm_WithNullQuestion_ShouldReturnErrorMessage()
    {
        // Arrange
        string question = null;
        _ollamaMock.Setup(x => x.IsHealthyAsync()).ReturnsAsync(true);
        _ollamaMock.Setup(x => x.IsModelAvailableAsync(It.IsAny<string>())).ReturnsAsync(true);
        _ollamaMock.Setup(x => x.GenerateAsync(question)).ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _askLlmTool.AskLlm(question);

        // Assert
        Assert.Contains("❌", result);
    }

    /// <summary>
    /// Проверяет, что метод AskLlm возвращает ошибку при возникновении исключения в OllamaService.
    /// Убеждается, что инструмент корректно обрабатывает ошибки и возвращает пользовательское сообщение.
    /// </summary>
    [Fact]
    public async Task AskLlm_WithOllamaServiceError_ShouldReturnErrorMessage()
    {
        // Arrange
        var question = "What is AI?";
        var testError = new Exception("Test exception");
        _ollamaMock.Setup(x => x.IsHealthyAsync()).ReturnsAsync(true);
        _ollamaMock.Setup(x => x.IsModelAvailableAsync(It.IsAny<string>())).ReturnsAsync(true);
        _ollamaMock.Setup(x => x.GenerateAsync(question)).ThrowsAsync(testError);

        // Act
        var result = await _askLlmTool.AskLlm(question);

        // Assert
        Assert.Contains("❌", result);
    }
}