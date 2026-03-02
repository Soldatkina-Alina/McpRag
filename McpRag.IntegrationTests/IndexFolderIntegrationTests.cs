using McpRag;
using McpRag.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using System.Threading;
using Xunit;

namespace McpRag.IntegrationTests;

/// <summary>
/// Интеграционный тест для IndexFolder - инструмента для индексации папок с файлами.
/// Проверяет базовую работу инструмента.
/// </summary>
public class IndexFolderIntegrationTests
{
    /// <summary>
    /// Проверяет базовую работу IndexFolder - вызов метода с передачей пути и паттерна.
    /// </summary>
    [Fact]
    public async Task IndexFolder_BasicFunctionality()
    {
        // Arrange
        var testFolder = "C:\\test_docs";
        
        var indexerLogger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<IndexerService>();
        var chromaLogger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<ChromaDbService>();
        var config = Options.Create(new IndexerConfig());
        
        var httpClient = new HttpClient { BaseAddress = new System.Uri("http://localhost:8000") };
        var chromaDbService = new ChromaDbService(httpClient, CreateMockOllamaService().Object, chromaLogger);
        
        var indexerService = new IndexerService(config, chromaDbService, CreateMockOllamaService().Object, indexerLogger);
        var toolsLogger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<IndexFolderTools>();
        var indexFolderTools = new IndexFolderTools(toolsLogger, indexerService);
        
        // Act
        var result = await indexFolderTools.IndexFolder(testFolder, "*.txt");
        
        // Assert
        // Просто проверяем, что метод не выбрасывает исключение и возвращает не пустую строку
        Assert.False(string.IsNullOrEmpty(result));
    }
    
    /// <summary>
    /// Создает мок для OllamaService.
    /// </summary>
    private static Moq.Mock<IOllamaService> CreateMockOllamaService()
    {
        var mock = new Moq.Mock<IOllamaService>();
        
        // Возвращаем фиктивные эмбеддинги
        mock.Setup(x => x.GenerateEmbeddingsAsync(Moq.It.IsAny<string>(), Moq.It.IsAny<CancellationToken>()))
            .Returns((string s, CancellationToken ct) => Task.FromResult(new float[10]));
            
        return mock;
    }
}