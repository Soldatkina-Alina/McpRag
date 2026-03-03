using McpRag;
using McpRag.Tools;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.IO;

namespace McpRag.Tests;

/// <summary>
/// Тесты для SummarizeDocumentTool - инструмента для создания краткого содержания документов.
/// Проверяют работу с файлами, генерацию саммари и обработку ошибок.
/// </summary>
public class SummarizeDocumentToolTests
{
    private readonly Mock<IIndexerService> _indexerMock;
    private readonly Mock<IOllamaService> _ollamaMock;
    private readonly SummarizeDocumentTool _summarizeDocumentTool;

    public SummarizeDocumentToolTests()
    {
        _indexerMock = new Mock<IIndexerService>();
        _ollamaMock = new Mock<IOllamaService>();
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<SummarizeDocumentTool>();
        _summarizeDocumentTool = new SummarizeDocumentTool(_indexerMock.Object, _ollamaMock.Object, logger);
    }

    /// <summary>
    /// Проверяет, что метод SummarizeDocument возвращает саммари при указании существующего файла.
    /// Убеждается, что результат содержит ожидаемую информацию и форматируется правильно.
    /// </summary>
    [Fact]
    public async Task SummarizeDocument_ShouldReturnSummary_ForExistingFile()
    {
        // Arrange
        var filePath = "test_document.txt";
        File.WriteAllText(filePath, "This is a test document content. It contains information about RAG systems.");

        var mockChunks = new List<DocumentChunk>
        {
            new DocumentChunk
            {
                Id = "1",
                Text = "This is the first part of the document. It contains information about RAG systems.",
                Source = filePath,
                ChunkIndex = 0
            },
            new DocumentChunk
            {
                Id = "2",
                Text = "The second part explains how RAG combines retrieval and generation.",
                Source = filePath,
                ChunkIndex = 1
            }
        };
        _indexerMock.Setup(x => x.LoadAndSplitDocumentAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockChunks);
        _ollamaMock.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("This document explains RAG systems that combine retrieval and generation.");

        // Act
        var result = await _summarizeDocumentTool.SummarizeDocument(filePath);

        // Cleanup
        File.Delete(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("📄 **Саммари документа: test_document.txt**", result);
        Assert.Contains("This document explains RAG systems", result);
        Assert.Contains("📊 **Статистика:**", result);
    }

    /// <summary>
    /// Проверяет, что метод SummarizeDocument возвращает ошибку при указании несуществующего файла.
    /// Убеждается, что инструмент корректно обрабатывает ситуацию с не найденным файлом.
    /// </summary>
    [Fact]
    public async Task SummarizeDocument_WithNonExistentFile_ShouldReturnError()
    {
        // Arrange
        var filePath = "nonexistent_file.txt";

        // Act
        var result = await _summarizeDocumentTool.SummarizeDocument(filePath);

        // Assert
        Assert.Contains("❌ Файл 'nonexistent_file.txt' не существует.", result);
    }

    /// <summary>
    /// Проверяет, что метод SummarizeDocument возвращает ошибку при возникновении исключения.
    /// Убеждается, что инструмент корректно обрабатывает ошибки и возвращает пользовательское сообщение.
    /// </summary>
    [Fact]
    public async Task SummarizeDocument_WithException_ShouldReturnErrorMessage()
    {
        // Arrange
        var filePath = "error_file.txt";
        File.WriteAllText(filePath, "Test content");

        var testError = new Exception("Test exception");
        _indexerMock.Setup(x => x.LoadAndSplitDocumentAsync(filePath, It.IsAny<CancellationToken>()))
            .ThrowsAsync(testError);

        // Act
        var result = await _summarizeDocumentTool.SummarizeDocument(filePath);

        // Cleanup
        File.Delete(filePath);

        // Assert
        Assert.Contains("❌ Ошибка: Test exception", result);
    }

    /// <summary>
    /// Проверяет, что метод SummarizeDocument обрабатывает пустой файл корректно.
    /// Убеждается, что инструмент не выбрасывает исключение и возвращает сообщение об ошибке.
    /// </summary>
    [Fact]
    public async Task SummarizeDocument_WithEmptyFile_ShouldReturnError()
    {
        // Arrange
        var filePath = "empty_file.txt";
        File.WriteAllText(filePath, string.Empty);

        var mockChunks = new List<DocumentChunk>();
        _indexerMock.Setup(x => x.LoadAndSplitDocumentAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockChunks);

        // Act
        var result = await _summarizeDocumentTool.SummarizeDocument(filePath);

        // Cleanup
        File.Delete(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("📄 **Саммари документа: empty_file.txt**", result);
    }
}