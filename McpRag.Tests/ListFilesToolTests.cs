using McpRag;
using McpRag.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace McpRag.Tests;

public class ListFilesToolTests
{
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
        Assert.Contains("file1.txt", result);
        Assert.Contains("file2.txt", result);
        Assert.Contains("large.txt", result);
    }
}