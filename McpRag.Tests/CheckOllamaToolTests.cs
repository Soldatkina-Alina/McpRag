using McpRag;
using McpRag.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace McpRag.Tests;

/// <summary>
/// Тесты для CheckOllamaTool - инструмента для проверки доступности Ollama сервиса.
/// Проверяют работу с доступным и недоступным сервисом.
/// </summary>
public class CheckOllamaToolTests
{
    private readonly Mock<IOllamaService> _ollamaMock;
    private readonly CheckOllamaTool _checkOllamaTool;

    public CheckOllamaToolTests()
    {
        _ollamaMock = new Mock<IOllamaService>();
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<CheckOllamaTool>();
        var config = Options.Create(new OllamaConfig { Model = "phi3:mini", EmbeddingModel = "nomic-embed-text", BaseUrl = "http://localhost:11434" });
        _checkOllamaTool = new CheckOllamaTool(_ollamaMock.Object, config, logger);
    }

    /// <summary>
    /// Проверяет, что метод CheckOllama возвращает сообщение о доступности Ollama сервиса.
    /// Убеждается, что результат содержит информацию о состоянии подключения и доступных моделях.
    /// </summary>
    [Fact]
    public async Task CheckOllama_ShouldReturnOllamaStatus()
    {
        // Arrange
        var availableModels = new List<string> { "llama2", "phi3" };
        _ollamaMock.Setup(x => x.IsHealthyAsync()).ReturnsAsync(true);
        _ollamaMock.Setup(x => x.ListModelsAsync()).ReturnsAsync(availableModels);
        _ollamaMock.Setup(x => x.IsModelAvailableAsync(It.IsAny<string>())).ReturnsAsync(true);

        // Act
        var result = await _checkOllamaTool.CheckOllama();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Ollama доступна", result);
        Assert.Contains("Доступные модели", result);
        Assert.Contains("llama2", result);
        Assert.Contains("phi3", result);
    }

    /// <summary>
    /// Проверяет, что метод CheckOllama возвращает сообщение об недоступности Ollama сервиса.
    /// Убеждается, что инструмент корректно обрабатывает ситуацию с недоступным сервисом.
    /// </summary>
    [Fact]
    public async Task CheckOllama_ShouldReturnOllamaUnavailable()
    {
        // Arrange
        _ollamaMock.Setup(x => x.IsHealthyAsync()).ReturnsAsync(false);

        // Act
        var result = await _checkOllamaTool.CheckOllama();

        // Assert
        Assert.Contains("Ollama не доступна", result);
    }
}