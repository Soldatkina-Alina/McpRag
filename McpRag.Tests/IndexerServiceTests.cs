using McpRag;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace McpRag.Tests;

/// <summary>
/// Тесты для IndexerService - сервиса для индексации документов.
/// Проверяют загрузку файлов, работу с загруженными файлами и очистку списка загруженных файлов.
/// </summary>
public class IndexerServiceTests
{
    /// <summary>
    /// Проверяет, что метод LoadFilesAsync правильно загружает файлы из существующей папки.
    /// Убеждается, что список загруженных файлов не пуст и содержит ожидаемые элементы.
    /// </summary>
    [Fact]
    public async Task LoadFilesAsync_ShouldLoadFiles_FromExistingFolder()
    {
        // Arrange
        var config = Options.Create(new IndexerConfig());
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<IndexerService>();
        var vectorStoreMock = new Mock<IVectorStoreService>();
        var ollamaMock = new Mock<IOllamaService>();
        var indexerService = new IndexerService(config, vectorStoreMock.Object, ollamaMock.Object, logger);
        var testFolder = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "test_docs");

        // Act
        var files = await indexerService.LoadFilesAsync(testFolder, "*.*", CancellationToken.None);

        // Assert
        Assert.NotNull(files);
        Assert.NotEmpty(files);
    }

    /// <summary>
    /// Проверяет, что метод LoadFilesAsync возвращает пустой список, если папка не существует.
    /// Убеждается, что сервис корректно обрабатывает ситуацию с несуществующей папкой.
    /// </summary>
    [Fact]
    public async Task LoadFilesAsync_ShouldReturnEmptyList_WhenFolderDoesNotExist()
    {
        // Arrange
        var config = Options.Create(new IndexerConfig());
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<IndexerService>();
        var vectorStoreMock = new Mock<IVectorStoreService>();
        var ollamaMock = new Mock<IOllamaService>();
        var indexerService = new IndexerService(config, vectorStoreMock.Object, ollamaMock.Object, logger);
        var testFolder = "non_existent_folder";

        // Act
        var files = await indexerService.LoadFilesAsync(testFolder, "*.*", CancellationToken.None);

        // Assert
        Assert.NotNull(files);
        Assert.Empty(files);
    }

    /// <summary>
    /// Проверяет, что метод LoadFilesAsync загружает только файлы с указанным расширением.
    /// Убеждается, что все загруженные файлы имеют ожидаемое расширение.
    /// </summary>
    [Fact]
    public async Task LoadFilesAsync_ShouldLoadFiles_WithSpecificExtension()
    {
        // Arrange
        var config = Options.Create(new IndexerConfig());
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<IndexerService>();
        var vectorStoreMock = new Mock<IVectorStoreService>();
        var ollamaMock = new Mock<IOllamaService>();
        var indexerService = new IndexerService(config, vectorStoreMock.Object, ollamaMock.Object, logger);
        var testFolder = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "test_docs");

        // Act
        var files = await indexerService.LoadFilesAsync(testFolder, "*.txt", CancellationToken.None);

        // Assert
        Assert.NotNull(files);
        Assert.NotEmpty(files);
        Assert.All(files, f => Assert.Equal(".txt", f.Extension));
    }

    /// <summary>
    /// Проверяет, что метод GetLoadedFiles возвращает список загруженных файлов.
    /// Убеждается, что после загрузки файлов метод возвращает не пустой список.
    /// </summary>
    [Fact]
    public async Task GetLoadedFiles_ShouldReturnLoadedFiles()
    {
        // Arrange
        var config = Options.Create(new IndexerConfig());
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<IndexerService>();
        var vectorStoreMock = new Mock<IVectorStoreService>();
        var ollamaMock = new Mock<IOllamaService>();
        var indexerService = new IndexerService(config, vectorStoreMock.Object, ollamaMock.Object, logger);
        var testFolder = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "test_docs");
        await indexerService.LoadFilesAsync(testFolder, "*.*", CancellationToken.None);

        // Act
        var loadedFiles = indexerService.GetLoadedFiles();

        // Assert
        Assert.NotNull(loadedFiles);
        Assert.NotEmpty(loadedFiles);
    }

    /// <summary>
    /// Проверяет, что метод ClearLoadedFiles очищает список загруженных файлов.
    /// Убеждается, что после вызова метода список становится пустым.
    /// </summary>
    [Fact]
    public async Task ClearLoadedFiles_ShouldClearLoadedFiles()
    {
        // Arrange
        var config = Options.Create(new IndexerConfig());
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<IndexerService>();
        var vectorStoreMock = new Mock<IVectorStoreService>();
        var ollamaMock = new Mock<IOllamaService>();
        var indexerService = new IndexerService(config, vectorStoreMock.Object, ollamaMock.Object, logger);
        var testFolder = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "test_docs");
        await indexerService.LoadFilesAsync(testFolder, "*.*", CancellationToken.None);

        // Act
        indexerService.ClearLoadedFiles();
        var loadedFiles = indexerService.GetLoadedFiles();

        // Assert
        Assert.NotNull(loadedFiles);
        Assert.Empty(loadedFiles);
    }
}