using McpRag;
using McpRag.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading;
using Xunit;

namespace McpRag.IntegrationTests;

/// <summary>
/// Интеграционные тесты для проверки работы с ChromaDB и IndexFolder.
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
        Assert.False(string.IsNullOrEmpty(result));
    }
    
    /// <summary>
    /// Проверяет работу ChromaDbService с реальными серверами (ChromaDB и Ollama).
    /// </summary>
    [Fact]
    public async Task ChromaDbService_RealServerIntegration()
    {
        // Arrange - используем реальные сервисы
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<ChromaDbService>();
        
        // Создаем реальный HttpClient для ChromaDB
        var httpClient = new HttpClient 
        { 
            BaseAddress = new Uri("http://localhost:8000"),
            Timeout = TimeSpan.FromSeconds(30)
        };
        
        // Создаем реальный OllamaService
        var ollamaLogger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<OllamaService>();
        var ollamaConfig = Options.Create(new OllamaConfig
        {
            BaseUrl = "http://localhost:11434",
            EmbeddingModel = "nomic-embed-text",
            Model = "phi3:mini",
            TimeoutSeconds = 30
        });
        var ollamaService = new OllamaService(httpClient, ollamaConfig, ollamaLogger);
        
        var chromaDbService = new ChromaDbService(httpClient, ollamaService, logger);
        
        // Создаем тестовые данные
        var testChunks = new[]
        {
            new DocumentChunk
            {
                Id = Guid.NewGuid().ToString(),
                Text = "Первый тестовый документ о искусственном интеллекте и машинном обучении",
                Source = "test_ai.txt",
                ChunkIndex = 0,
                IndexedAt = DateTime.Now
            },
            new DocumentChunk
            {
                Id = Guid.NewGuid().ToString(),
                Text = "Второй тестовый документ о векторных базах данных и поиске по семантике",
                Source = "test_vector_dbs.txt",
                ChunkIndex = 0,
                IndexedAt = DateTime.Now
            },
            new DocumentChunk
            {
                Id = Guid.NewGuid().ToString(),
                Text = "Третий тестовый документ о системах ретриевального-Augmented Generation (RAG)",
                Source = "test_rag.txt",
                ChunkIndex = 0,
                IndexedAt = DateTime.Now
            }
        };
        
        // Act - выполняем реальные операции
        try
        {
            // Проверка доступности ChromaDB
            var countBefore = await chromaDbService.CountAsync();
            Console.WriteLine($"Количество документов перед добавлением: {countBefore}");
            
            // Добавление тестовых данных
            await chromaDbService.AddDocumentsAsync(testChunks);
            Console.WriteLine("Тестовые данные успешно добавлены");
            
            // Проверка количества документов
            var countAfter = await chromaDbService.CountAsync();
            Console.WriteLine($"Количество документов после добавления: {countAfter}");
            
            // Поиск по тестовому запросу
            var results = await chromaDbService.SearchAsync("искусственный интеллект", 2);
            Console.WriteLine($"Найдено {results.Count()} результатов для запроса 'искусственный интеллект'");
            
            // Assert - проверки
            Assert.True(countAfter > countBefore, "Количество документов должно увеличиться");
            Assert.NotEmpty(results);
            foreach (var result in results)
            {
                Assert.NotNull(result.Text);
                Assert.NotNull(result.Source);
                Assert.True(result.Text.Length > 0);
            }
            
            Console.WriteLine("Тестирование ChromaDB завершено успешно!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при тестировании ChromaDB: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Внутренняя ошибка: {ex.InnerException.Message}");
            }
            Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
            
            Assert.Fail($"Тест провален: {ex.Message}");
        }
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
