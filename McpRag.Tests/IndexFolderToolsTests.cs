using McpRag;
using McpRag.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace McpRag.Tests;

/// <summary>
/// Тесты для IndexFolderTools - инструмента для индексации папок с файлами.
/// Проверяют работу с существующими и несуществующими папками.
/// </summary>
public class IndexFolderToolsTests
{
    /// <summary>
    /// Проверяет, что метод IndexFolder возвращает количество файлов при индексации существующей папки.
    /// Убеждается, что результат содержит информацию о загруженных файлах.
    /// </summary>
    [Fact]
    public async Task IndexFolder_ShouldReturnFileCount_ForExistingFolder()
    {
        // Arrange
        var config = Options.Create(new IndexerConfig());
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<IndexerService>();
        var vectorStoreMock = new Mock<IVectorStoreService>();
        var ollamaMock = new Mock<IOllamaService>();
        var indexerService = new IndexerService(config, vectorStoreMock.Object, ollamaMock.Object, logger);
        var toolsLogger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<IndexFolderTools>();
        var indexFolderTools = new IndexFolderTools(toolsLogger, indexerService);
        var testFolder = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "test_docs");

        // Act
        var result = await indexFolderTools.IndexFolder(testFolder, "*.*");

        // Assert
        Assert.Contains("Загружено", result);
    }

    /// <summary>
    /// Проверяет, что метод IndexFolder возвращает сообщение об ошибке при попытке индексации несуществующей папки.
    /// Убеждается, что инструмент корректно обрабатывает ошибку и возвращает соответствующее сообщение.
    /// </summary>
    [Fact]
    public async Task IndexFolder_ShouldReturnError_ForNonExistingFolder()
    {
        // Arrange
        var config = Options.Create(new IndexerConfig());
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<IndexerService>();
        var vectorStoreMock = new Mock<IVectorStoreService>();
        var ollamaMock = new Mock<IOllamaService>();
        var indexerService = new IndexerService(config, vectorStoreMock.Object, ollamaMock.Object, logger);
        var toolsLogger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<IndexFolderTools>();
        var indexFolderTools = new IndexFolderTools(toolsLogger, indexerService);
        var testFolder = "non_existent_folder";

        // Act
        var result = await indexFolderTools.IndexFolder(testFolder, "*.*");

        // Assert
        Assert.Contains("Ошибка", result);
    }
}