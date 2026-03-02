using McpRag;
using McpRag.Tools;
using Microsoft.Extensions.Logging;
using Xunit;

namespace McpRag.Tests;

/// <summary>
/// Тесты для EchoTools - инструмента для эхо-ответа.
/// Проверяют работу с разными входными данными и сценариями.
/// </summary>
public class EchoToolsTests
{
    private readonly EchoTools _echoTools;

    public EchoToolsTests()
    {
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<EchoTools>();
        _echoTools = new EchoTools(logger);
    }

    /// <summary>
    /// Проверяет, что метод Echo возвращает правильный ответ для текстового сообщения.
    /// Убеждается, что ответ содержит введенное сообщение.
    /// </summary>
    [Fact]
    public void Echo_ShouldReturnMessageForNonEmptyString()
    {
        // Arrange
        string message = "Test message";

        // Act
        string result = _echoTools.Echo(message);

        // Assert
        Assert.Equal($"Echo: {message}", result);
    }

    /// <summary>
    /// Проверяет, что метод Echo обрабатывает пустую строку корректно.
    /// Убеждается, что инструмент возвращает ответ без ошибок при пустом входном параметре.
    /// </summary>
    [Fact]
    public void Echo_ShouldHandleEmptyString()
    {
        // Arrange
        string message = string.Empty;

        // Act
        string result = _echoTools.Echo(message);

        // Assert
        Assert.Equal("Echo: ", result);
    }

    /// <summary>
    /// Проверяет, что метод Echo обрабатывает null сообщение.
    /// Убеждается, что инструмент возвращает ответ без ошибок при null входном параметре.
    /// </summary>
    [Fact]
    public void Echo_ShouldHandleNull()
    {
        // Arrange
        string message = null;

        // Act
        string result = _echoTools.Echo(message);

        // Assert
        Assert.Equal("Echo: ", result);
    }
}