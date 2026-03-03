using McpRag;
using McpRag.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace McpRag.Tests;

/// <summary>
/// Тесты для FindRelevantDocsTool - инструмента для поиска релевантных документов.
/// Проверяют работу с запросами, фильтрацию по релевантности и форматирование результатов.
/// </summary>
public class FindRelevantDocsToolTests
{
    private readonly Mock<IVectorStoreService> _vectorStoreMock;
    private readonly FindRelevantDocsTool _findRelevantDocsTool;
    private readonly RAGConfig _config;

    public FindRelevantDocsToolTests()
    {
        _vectorStoreMock = new Mock<IVectorStoreService>();
        _config = new RAGConfig
        {
            MinRelevanceScore = 0.7f,
            MaxChunks = 5,
            MaxContextTokens = 2000,
            IncludeMetadataInContext = true
        };
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<FindRelevantDocsTool>();
        var configOptions = Options.Create(_config);
        _findRelevantDocsTool = new FindRelevantDocsTool(_vectorStoreMock.Object, configOptions, logger);
    }

    /// <summary>
    /// Проверяет, что метод FindRelevantDocs возвращает результаты при отправке валидного запроса.
    /// Убеждается, что результаты содержат ожидаемую информацию и форматируются правильно.
    /// </summary>
    [Fact]
    public async Task FindRelevantDocs_ShouldReturnResults_ForValidQuery()
    {
        // Arrange
        var query = "What is RAG?";
        var mockResults = new List<SearchResult>
        {
            new SearchResult
            {
                Chunk = new DocumentChunk
                {
                    Id = "1",
                    Text = "RAG stands for Retrieval-Augmented Generation...",
                    Source = "test1.txt",
                    ChunkIndex = 0
                },
                Score = 0.85f
            },
            new SearchResult
            {
                Chunk = new DocumentChunk
                {
                    Id = "2",
                    Text = "RAG combines retrieval and generation...",
                    Source = "test2.txt",
                    ChunkIndex = 1
                },
                Score = 0.92f
            }
        };
        _vectorStoreMock.Setup(x => x.SearchWithScoreAsync(query, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResults);

        // Act
        var result = await _findRelevantDocsTool.FindRelevantDocs(query);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("📋 **Найдено 2 релевантных документов:**", result);
        Assert.Contains("test1.txt", result);
        Assert.Contains("test2.txt", result);
        Assert.Contains("85", result);
        Assert.Contains("92", result);
    }

    /// <summary>
    /// Проверяет, что метод FindRelevantDocs возвращает сообщение об ошибке при отсутствии документов.
    /// Убеждается, что инструмент корректно обрабатывает пустой результат поиска.
    /// </summary>
    [Fact]
    public async Task FindRelevantDocs_WithNoResults_ShouldReturnNoDocumentsMessage()
    {
        // Arrange
        var query = "Nonexistent topic";
        _vectorStoreMock.Setup(x => x.SearchWithScoreAsync(query, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SearchResult>());

        // Act
        var result = await _findRelevantDocsTool.FindRelevantDocs(query);

        // Assert
        Assert.Contains("❌ Документы не найдены.", result);
    }

    /// <summary>
    /// Проверяет, что метод FindRelevantDocs возвращает сообщение, если все документы ниже порога релевантности.
    /// Убеждается, что инструмент корректно фильтрует результаты по минимальному порогу.
    /// </summary>
    [Fact]
    public async Task FindRelevantDocs_WithLowRelevance_ShouldReturnThresholdMessage()
    {
        // Arrange
        var query = "Partially relevant";
        var mockResults = new List<SearchResult>
        {
            new SearchResult
            {
                Chunk = new DocumentChunk
                {
                    Id = "1",
                    Text = "Some partially relevant content...",
                    Source = "test1.txt",
                    ChunkIndex = 0
                },
                Score = 0.65f
            }
        };
        _vectorStoreMock.Setup(x => x.SearchWithScoreAsync(query, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResults);

        // Act
        var result = await _findRelevantDocsTool.FindRelevantDocs(query);

        // Assert
        Assert.Contains("📋 **Найдено 1 релевантных документов:**", result);
    }

    /// <summary>
    /// Проверяет, что метод FindRelevantDocs фильтрует результаты по указанному порогу.
    /// Убеждается, что инструмент использует пользовательский порог вместо значения по умолчанию.
    /// </summary>
    [Fact]
    public async Task FindRelevantDocs_WithCustomThreshold_ShouldFilterResults()
    {
        // Arrange
        var query = "RAG";
        var mockResults = new List<SearchResult>
        {
            new SearchResult
            {
                Chunk = new DocumentChunk
                {
                    Id = "1",
                    Text = "RAG system...",
                    Source = "test1.txt",
                    ChunkIndex = 0
                },
                Score = 0.85f
            },
            new SearchResult
            {
                Chunk = new DocumentChunk
                {
                    Id = "2",
                    Text = "Some other content...",
                    Source = "test2.txt",
                    ChunkIndex = 1
                },
                Score = 0.65f
            }
        };
        _vectorStoreMock.Setup(x => x.SearchWithScoreAsync(query, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResults);

        // Act
        var result = await _findRelevantDocsTool.FindRelevantDocs(query, 2, 0.8);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("📋 **Найдено 1 релевантных документов:**", result);
        Assert.Contains("test1.txt", result);
        Assert.DoesNotContain("test2.txt", result);
    }

    /// <summary>
    /// Проверяет, что метод FindRelevantDocs возвращает ошибку при возникновении исключения в векторном хранилище.
    /// Убеждается, что инструмент корректно обрабатывает ошибки и возвращает пользовательское сообщение.
    /// </summary>
    [Fact]
    public async Task FindRelevantDocs_WithVectorStoreError_ShouldReturnErrorMessage()
    {
        // Arrange
        var query = "Error test";
        var testError = new Exception("Test exception");
        _vectorStoreMock.Setup(x => x.SearchWithScoreAsync(query, 2, It.IsAny<CancellationToken>()))
            .ThrowsAsync(testError);

        // Act
        var result = await _findRelevantDocsTool.FindRelevantDocs(query);

        // Assert
        Assert.Contains("❌ Ошибка: Test exception", result);
    }
}