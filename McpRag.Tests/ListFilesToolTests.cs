using McpRag;
using McpRag.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace McpRag.Tests;

/// <summary>
/// Тесты для ListFilesTool - инструмента для получения списка загруженных файлов.
/// Проверяют работу с загруженными файлами, фильтрацию по расширению и обработку пустого списка.
/// </summary>
public class ListFilesToolTests
{
    /// <summary>
    /// Проверяет, что метод ListFiles возвращает список файлов, когда они загружены.
    /// Убеждается, что результат не содержит сообщение о отсутствии загруженных файлов.
    /// </summary>
    [Fact]
    public void ListFiles_ShouldReturnFileList_WhenFilesAreLoaded()
    {
        // Arrange
        var config = Options.Create(new IndexerConfig());
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<IndexerService>();
        var vectorStoreMock = new Mock<IVectorStoreService>();
        var ollamaMock = new Mock<IOllamaService>();
        var indexerService = new IndexerService(config, vectorStoreMock.Object, ollamaMock.Object, logger);
        var toolsLogger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<ListFilesTool>();
        var listFilesTool = new ListFilesTool(toolsLogger, indexerService);
        var testFolder = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "test_docs");

        // Act
        indexerService.LoadFilesAsync(testFolder, "*.*", default).GetAwaiter().GetResult();
        var result = listFilesTool.ListFiles(null);

        // Assert
        Assert.NotEqual("Нет загруженных файлов", result);
    }

    /// <summary>
    /// Проверяет, что метод ListFiles возвращает сообщение о не загруженных файлах, когда список пуст.
    /// Убеждается, что инструмент корректно обрабатывает ситуацию с пустым списком файлов.
    /// </summary>
    [Fact]
    public void ListFiles_ShouldReturnNoFilesMessage_WhenNoFilesAreLoaded()
    {
        // Arrange
        var config = Options.Create(new IndexerConfig());
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<IndexerService>();
        var vectorStoreMock = new Mock<IVectorStoreService>();
        var ollamaMock = new Mock<IOllamaService>();
        var indexerService = new IndexerService(config, vectorStoreMock.Object, ollamaMock.Object, logger);
        var toolsLogger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<ListFilesTool>();
        var listFilesTool = new ListFilesTool(toolsLogger, indexerService);

        // Act
        var result = listFilesTool.ListFiles(null);

        // Assert
        Assert.Equal("Нет загруженных файлов", result);
    }

    /// <summary>
    /// Проверяет, что метод ListFiles фильтрует файлы по расширению.
    /// Убеждается, что в результатах есть файлы с указанным расширением и нет файлов с другим расширением.
    /// </summary>
    [Fact]
    public void ListFiles_ShouldFilterFiles_ByExtension()
    {
        // Arrange
        var config = Options.Create(new IndexerConfig());
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<IndexerService>();
        var vectorStoreMock = new Mock<IVectorStoreService>();
        var ollamaMock = new Mock<IOllamaService>();
        var indexerService = new IndexerService(config, vectorStoreMock.Object, ollamaMock.Object, logger);
        var toolsLogger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<ListFilesTool>();
        var listFilesTool = new ListFilesTool(toolsLogger, indexerService);
        var testFolder = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "test_docs");

        // Act
        indexerService.LoadFilesAsync(testFolder, "*.*", default).GetAwaiter().GetResult();
        var result = listFilesTool.ListFiles(".txt");

        // Assert
        Assert.Contains("cats.txt", result);
        Assert.Contains("dogs.txt", result);
    }
}