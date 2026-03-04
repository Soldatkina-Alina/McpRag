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
        var indexerConfig = Options.Create(new IndexerConfig());
        var ragConfig = Options.Create(new RAGConfig());
        
        var httpClient = new HttpClient { BaseAddress = new System.Uri("http://localhost:8000") };
        var vectorStoreConfig = Options.Create(new VectorStoreConfig
        {
            ConnectionString = "http://localhost:8000"
        });
        var chromaDbService = new ChromaDbService(httpClient, CreateMockOllamaService().Object, vectorStoreConfig, ragConfig, chromaLogger);
        // Создаем реальный HttpClient для Ollama
        var ollamaHttpClient = new HttpClient();
        var ollamaLogger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<OllamaService>();
        var ollamaConfig = Options.Create(new OllamaConfig
        {
            BaseUrl = "http://localhost:11434",
            EmbeddingModel = "nomic-embed-text",
            Model = "phi3:mini",
            TimeoutSeconds = 30
        });
        var ollamaService = new OllamaService(ollamaHttpClient, ollamaConfig, ollamaLogger);

        var indexerService = new IndexerService(indexerConfig, chromaDbService, ollamaService, indexerLogger);
        var toolsLogger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<IndexFolderTools>();
        var indexFolderTools = new IndexFolderTools(toolsLogger, indexerService);

        // Act
        await chromaDbService.ClearAsync();
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
        
        // Создаем реальный HttpClient для Ollama
        var ollamaHttpClient = new HttpClient();
        var ollamaLogger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<OllamaService>();
        var ollamaConfig = Options.Create(new OllamaConfig
        {
            BaseUrl = "http://localhost:11434",
            EmbeddingModel = "nomic-embed-text",
            Model = "phi3:mini",
            TimeoutSeconds = 30
        });
        var ollamaService = new OllamaService(ollamaHttpClient, ollamaConfig, ollamaLogger);
        
        var vectorStoreConfig = Options.Create(new VectorStoreConfig
        {
            ConnectionString = "http://localhost:8000"
        });
        var ragConfig = Options.Create(new RAGConfig());
        var chromaDbService = new ChromaDbService(httpClient, ollamaService, vectorStoreConfig, ragConfig, logger);
        
        // Создаем тестовые данные с реальными эмбеддингами
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
                Text = "Третий тестовый документ о собачках. Собаки любят уток",
                Source = "test_rag.txt",
                ChunkIndex = 0,
                IndexedAt = DateTime.Now
            }
        };
        
        // Генерируем эмбеддинги для тестовых данных
        var chunksWithEmbeddings = new List<DocumentChunk>();
        foreach (var chunk in testChunks)
        {
            var embedding = await ollamaService.GenerateEmbeddingsAsync(chunk.Text);
            chunksWithEmbeddings.Add(new DocumentChunk
            {
                Id = chunk.Id,
                Text = chunk.Text,
                Source = chunk.Source,
                ChunkIndex = chunk.ChunkIndex,
                IndexedAt = chunk.IndexedAt,
                Embedding = embedding
            });
            Console.WriteLine($"Генерация эмбеддингов для текста: {chunk.Text.Substring(0, Math.Min(50, chunk.Text.Length))}...");
        }

        // Act - выполняем реальные операции
        try
        {
            // Проверка доступности ChromaDB
            var countBefore = await chromaDbService.CountAsync();
            Console.WriteLine($"Количество документов перед добавлением: {countBefore}");

            // Добавление тестовых данных
            //await chromaDbService.ClearAsync();
            await chromaDbService.AddDocumentsAsync(chunksWithEmbeddings);
            //Console.WriteLine("Тестовые данные успешно добавлены");
            
            // Проверка количества документов
            //var countAfter = await chromaDbService.CountAsync();
            //Console.WriteLine($"Количество документов после добавления: {countAfter}");
            
            // Поиск по тестовому запросу
            var results = await chromaDbService.SearchAsync("Собаки любят", 1);
            Console.WriteLine($"Найдено {results.Count()} результатов для запроса 'Собаки любят'");
            
            // Assert - проверки
            //Assert.True(countAfter > countBefore, "Количество документов должно увеличиться");
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
    /// Проверяет работу ListFiles - вызов метода с фильтрацией по расширению.
    /// </summary>
    [Fact]
    public async Task ListFiles_BasicFunctionality()
    {
        // Arrange
        var testFolder = "C:\\test_docs";
        
        var indexerLogger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<IndexerService>();
        var chromaLogger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<ChromaDbService>();
        var config = Options.Create(new IndexerConfig());
        
        var httpClient = new HttpClient { BaseAddress = new System.Uri("http://localhost:8000") };
        var vectorStoreConfig = Options.Create(new VectorStoreConfig
        {
            ConnectionString = "http://localhost:8000"
        });
        var ragConfig = Options.Create(new RAGConfig());
        var chromaDbService = new ChromaDbService(httpClient, CreateMockOllamaService().Object, vectorStoreConfig, ragConfig, chromaLogger);
        
        var indexerService = new IndexerService(config, chromaDbService, CreateMockOllamaService().Object, indexerLogger);
        var toolsLogger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<ListFilesTool>();
        var listFilesTool = new ListFilesTool(toolsLogger, indexerService);
        
        // Act - Load files first to ensure we have something to list
        await indexerService.LoadFilesAsync(testFolder, "*.txt");
        var result = listFilesTool.ListFiles();
        
        // Assert
        Assert.False(string.IsNullOrEmpty(result));
        Assert.DoesNotContain("Нет загруженных файлов", result);
    }
    
    /// <summary>
    /// Проверяет работу ListFiles с фильтрацией по расширению.
    /// </summary>
    [Fact]
    public async Task ListFiles_WithExtensionFilter()
    {
        // Arrange
        var testFolder = "C:\\test_docs";
        
        var indexerLogger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<IndexerService>();
        var chromaLogger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<ChromaDbService>();
        var config = Options.Create(new IndexerConfig());
        
        var httpClient = new HttpClient { BaseAddress = new System.Uri("http://localhost:8000") };
        var vectorStoreConfig = Options.Create(new VectorStoreConfig
        {
            ConnectionString = "http://localhost:8000"
        });
        var ragConfig = Options.Create(new RAGConfig());
        var chromaDbService = new ChromaDbService(httpClient, CreateMockOllamaService().Object, vectorStoreConfig, ragConfig, chromaLogger);
        
        var indexerService = new IndexerService(config, chromaDbService, CreateMockOllamaService().Object, indexerLogger);
        var toolsLogger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<ListFilesTool>();
        var listFilesTool = new ListFilesTool(toolsLogger, indexerService);
        
        // Act - Load files first to ensure we have something to list
        await indexerService.LoadFilesAsync(testFolder, "*.*");
        var result = listFilesTool.ListFiles(".txt");
        
        // Assert
        Assert.False(string.IsNullOrEmpty(result));
    }
    
    /// <summary>
    /// Проверяет работу ClearVectorStore - очищает векторное хранилище и проверяет, что оно пустое.
    /// </summary>
    [Fact]
    public async Task ClearVectorStore_ShouldClearAllDocuments()
    {
        // Arrange
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<ChromaDbService>();
        
        // Создаем реальный HttpClient для ChromaDB
        var httpClient = new HttpClient 
        { 
            BaseAddress = new Uri("http://localhost:8000"),
            Timeout = TimeSpan.FromSeconds(30)
        };
        
        // Создаем реальный HttpClient для Ollama
        var ollamaHttpClient = new HttpClient();
        var ollamaLogger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<OllamaService>();
        var ollamaConfig = Options.Create(new OllamaConfig
        {
            BaseUrl = "http://localhost:11434",
            EmbeddingModel = "nomic-embed-text",
            Model = "phi3:mini",
            TimeoutSeconds = 30
        });
        var ollamaService = new OllamaService(ollamaHttpClient, ollamaConfig, ollamaLogger);
        
        var vectorStoreConfig = Options.Create(new VectorStoreConfig
        {
            ConnectionString = "http://localhost:8000"
        });
        var ragConfig = Options.Create(new RAGConfig());
        var chromaDbService = new ChromaDbService(httpClient, ollamaService, vectorStoreConfig, ragConfig, logger);
        
        // Создаем тестовые данные с реальными эмбеддингами
        var testChunks = new[]
        {
            new DocumentChunk
            {
                Id = Guid.NewGuid().ToString(),
                Text = "Тестовый документ для проверки очистки хранилища",
                Source = "test_clear.txt",
                ChunkIndex = 0,
                IndexedAt = DateTime.Now
            }
        };
        
        // Генерируем эмбеддинги для тестовых данных
        var chunksWithEmbeddings = new List<DocumentChunk>();
        foreach (var chunk in testChunks)
        {
            var embedding = await ollamaService.GenerateEmbeddingsAsync(chunk.Text);
            chunksWithEmbeddings.Add(new DocumentChunk
            {
                Id = chunk.Id,
                Text = chunk.Text,
                Source = chunk.Source,
                ChunkIndex = chunk.ChunkIndex,
                IndexedAt = chunk.IndexedAt,
                Embedding = embedding
            });
        }
        
        // Act - выполняем реальные операции
        try
        {
            // Добавление тестовых данных для очистки
            await chromaDbService.AddDocumentsAsync(chunksWithEmbeddings);

            // Проверка, что документы добавлены
            var countBeforeClear = await chromaDbService.CountAsync();
            Console.WriteLine($"Количество документов перед очисткой: {countBeforeClear}");

            // Создание инструмента для очистки
            var vectorStoreStatusLogger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<VectorStoreStatusTool>();
            var vectorStoreStatusTool = new VectorStoreStatusTool(chromaDbService, vectorStoreStatusLogger);
            
            // Очистка векторного хранилища
            var clearResult = await vectorStoreStatusTool.ClearVectorStore();
            Console.WriteLine($"Результат очистки: {clearResult}");
            
            // Проверка, что хранилище пустое
            var countAfterClear = await chromaDbService.CountAsync();
            Console.WriteLine($"Количество документов после очистки: {countAfterClear}");
            
            // Assert - проверки
            Assert.Equal("✅ Векторное хранилище успешно очищено", clearResult);
            Assert.Equal(0, countAfterClear);
            
            Console.WriteLine("Тестирование ClearVectorStore завершено успешно!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при тестировании ClearVectorStore: {ex.Message}");
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
