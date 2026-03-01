using McpRag.Tools;
using McpRag;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace McpRag.Tests;

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