using McpRag;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace McpRag.Tests;

public class IndexerServiceTests
{
    [Fact]
    public async Task LoadFilesAsync_ShouldLoadFiles_FromExistingFolder()
    {
        // Arrange
        var config = Options.Create(new IndexerConfig());
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<IndexerService>();
        var indexerService = new IndexerService(config, logger);
        var testFolder = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "test_docs");

        // Act
        var files = await indexerService.LoadFilesAsync(testFolder, "*.*", CancellationToken.None);

        // Assert
        Assert.NotNull(files);
        Assert.NotEmpty(files);
    }

    [Fact]
    public async Task LoadFilesAsync_ShouldReturnEmptyList_WhenFolderDoesNotExist()
    {
        // Arrange
        var config = Options.Create(new IndexerConfig());
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<IndexerService>();
        var indexerService = new IndexerService(config, logger);
        var testFolder = "non_existent_folder";

        // Act
        var files = await indexerService.LoadFilesAsync(testFolder, "*.*", CancellationToken.None);

        // Assert
        Assert.NotNull(files);
        Assert.Empty(files);
    }

    [Fact]
    public async Task LoadFilesAsync_ShouldLoadFiles_WithSpecificExtension()
    {
        // Arrange
        var config = Options.Create(new IndexerConfig());
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<IndexerService>();
        var indexerService = new IndexerService(config, logger);
        var testFolder = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "test_docs");

        // Act
        var files = await indexerService.LoadFilesAsync(testFolder, "*.txt", CancellationToken.None);

        // Assert
        Assert.NotNull(files);
        Assert.NotEmpty(files);
        Assert.All(files, f => Assert.Equal(".txt", f.Extension));
    }

    [Fact]
    public async Task GetLoadedFiles_ShouldReturnLoadedFiles()
    {
        // Arrange
        var config = Options.Create(new IndexerConfig());
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<IndexerService>();
        var indexerService = new IndexerService(config, logger);
        var testFolder = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "test_docs");
        await indexerService.LoadFilesAsync(testFolder, "*.*", CancellationToken.None);

        // Act
        var loadedFiles = indexerService.GetLoadedFiles();

        // Assert
        Assert.NotNull(loadedFiles);
        Assert.NotEmpty(loadedFiles);
    }

    [Fact]
    public async Task ClearLoadedFiles_ShouldClearLoadedFiles()
    {
        // Arrange
        var config = Options.Create(new IndexerConfig());
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<IndexerService>();
        var indexerService = new IndexerService(config, logger);
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