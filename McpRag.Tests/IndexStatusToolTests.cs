using McpRag;
using McpRag.Tools;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace McpRag.Tests;

/// <summary>
/// Тесты для IndexStatusTool - инструмента для получения статуса индекса.
/// Проверяют работу с статистикой индекса, пустым индексом и ошибками.
/// </summary>
public class IndexStatusToolTests
{
    private readonly Mock<IVectorStoreService> _vectorStoreMock;
    private readonly IndexStatusTool _indexStatusTool;

    public IndexStatusToolTests()
    {
        _vectorStoreMock = new Mock<IVectorStoreService>();
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<IndexStatusTool>();
        _indexStatusTool = new IndexStatusTool(_vectorStoreMock.Object, logger);
    }

    /// <summary>
    /// Проверяет, что метод IndexStatus возвращает статистику при наличии документов в индексе.
    /// Убеждается, что результат содержит ожидаемую информацию и форматируется правильно.
    /// </summary>
    [Fact]
    public async Task IndexStatus_ShouldReturnStatistics_WhenIndexHasDocuments()
    {
        // Arrange
        var mockStats = new IndexStatistics
        {
            TotalChunks = 10,
            TotalFiles = 2,
            LastIndexed = System.DateTime.Now.AddHours(-1),
            Collections = new List<CollectionInfo>
            {
                new CollectionInfo
                {
                    Name = "documents",
                    Count = 10,
                    Created = System.DateTime.Now.AddDays(-7)
                }
            }
        };
        _vectorStoreMock.Setup(x => x.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockStats);

        // Act
        var result = await _indexStatusTool.IndexStatus();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("📊 **Статус индекса:**", result);
        Assert.Contains("📁 **Всего файлов:** 2", result);
        Assert.Contains("📄 **Всего чанков:** 10", result);
        Assert.Contains("🗂️ **Коллекции ChromaDB:**", result);
        Assert.Contains("documents: 10 документов", result);
    }

    /// <summary>
    /// Проверяет, что метод IndexStatus возвращает сообщение о пустом индексе при отсутствии документов.
    /// Убеждается, что инструмент корректно обрабатывает пустой индекс.
    /// </summary>
    [Fact]
    public async Task IndexStatus_WithEmptyIndex_ShouldReturnEmptyIndexMessage()
    {
        // Arrange
        var mockStats = new IndexStatistics
        {
            TotalChunks = 0,
            TotalFiles = 0,
            LastIndexed = null,
            Collections = new List<CollectionInfo>()
        };
        _vectorStoreMock.Setup(x => x.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockStats);

        // Act
        var result = await _indexStatusTool.IndexStatus();

        // Assert
        Assert.Contains("❌ Индекс пуст. Выполните `index_folder` для индексации документов.", result);
    }

    /// <summary>
    /// Проверяет, что метод IndexStatus возвращает ошибку при возникновении исключения в векторном хранилище.
    /// Убеждается, что инструмент корректно обрабатывает ошибки и возвращает пользовательское сообщение.
    /// </summary>
    [Fact]
    public async Task IndexStatus_WithVectorStoreError_ShouldReturnErrorMessage()
    {
        // Arrange
        var testError = new Exception("Test exception");
        _vectorStoreMock.Setup(x => x.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(testError);

        // Act
        var result = await _indexStatusTool.IndexStatus();

        // Assert
        Assert.Contains("❌ Ошибка при получении статуса: Test exception", result);
    }

    /// <summary>
    /// Проверяет, что метод IndexStatus корректно отображает информацию о последней индексации.
    /// Убеждается, что результат содержит информацию о времени последней индексации.
    /// </summary>
    [Fact]
    public async Task IndexStatus_ShouldShowLastIndexed_WhenAvailable()
    {
        // Arrange
        var lastIndexed = System.DateTime.Now.AddHours(-2);
        var mockStats = new IndexStatistics
        {
            TotalChunks = 5,
            TotalFiles = 1,
            LastIndexed = lastIndexed,
            Collections = new List<CollectionInfo>()
        };
        _vectorStoreMock.Setup(x => x.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockStats);

        // Act
        var result = await _indexStatusTool.IndexStatus();

        // Assert
        Assert.Contains("🕒 **Последняя индексация:**", result);
        Assert.Contains("⏱️ **Прошло:**", result);
    }
}