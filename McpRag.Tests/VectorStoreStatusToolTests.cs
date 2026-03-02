using McpRag.Tools;
using McpRag;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace McpRag.Tests;

/// <summary>
/// Тесты для VectorStoreStatusTool - инструмента для проверки статуса векторного хранилища ChromaDB.
/// Проверяют получение статуса и очистку хранилища.
/// </summary>
public class VectorStoreStatusToolTests
{
    private readonly Mock<IVectorStoreService> _vectorStoreMock;
    private readonly VectorStoreStatusTool _vectorStoreStatusTool;

    public VectorStoreStatusToolTests()
    {
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<VectorStoreStatusTool>();
        _vectorStoreMock = new Mock<IVectorStoreService>();
        _vectorStoreStatusTool = new VectorStoreStatusTool(_vectorStoreMock.Object, logger);
    }

    /// <summary>
    /// Проверяет, что метод VectorStoreStatus возвращает правильный статус векторного хранилища.
    /// Убеждается, что статус содержит количество документов и другие необходимые данные.
    /// </summary>
    [Fact]
    public async Task VectorStoreStatus_ShouldReturnStatus()
    {
        // Arrange
        _vectorStoreMock.Setup(x => x.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        // Act
        var result = await _vectorStoreStatusTool.VectorStoreStatus();

        // Assert
        Assert.Contains("ChromaDB статус", result);
        Assert.Contains("Количество документов: 5", result);
    }

    /// <summary>
    /// Проверяет, что метод ClearVectorStore корректно отправляет запрос на очистку хранилища.
    /// Убеждается, что запрос отправляется и инструмент возвращает сообщение о успешной очистке.
    /// </summary>
    [Fact]
    public async Task ClearVectorStore_ShouldClearDocuments()
    {
        // Arrange
        var wasCalled = false;
        _vectorStoreMock.Setup(x => x.ClearAsync(It.IsAny<CancellationToken>()))
            .Callback(() => wasCalled = true);

        // Act
        var result = await _vectorStoreStatusTool.ClearVectorStore();

        // Assert
        Assert.Contains("Векторное хранилище успешно очищено", result);
        Assert.True(wasCalled);
    }

    /// <summary>
    /// Проверяет, что метод ClearVectorStore возвращает сообщение об ошибке при возникновении исключения.
    /// Убеждается, что инструмент корректно обрабатывает ошибки при очистке хранилища.
    /// </summary>
    [Fact]
    public async Task ClearVectorStore_WithError_ShouldReturnErrorMessage()
    {
        // Arrange
        var testError = new Exception("Test exception");
        _vectorStoreMock.Setup(x => x.ClearAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(testError);

        // Act
        var result = await _vectorStoreStatusTool.ClearVectorStore();

        // Assert
        Assert.Contains("Ошибка при очистке векторного хранилища", result);
        Assert.Contains(testError.Message, result);
    }
}