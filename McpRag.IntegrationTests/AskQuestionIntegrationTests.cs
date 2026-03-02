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
/// Интеграционные тесты для проверки работы с AskQuestionTool.
/// </summary>
public class AskQuestionIntegrationTests
{
    /// <summary>
    /// Проверяет, что AskQuestionTool выдает результат для вопроса с существующей информацией в документах.
    /// </summary>
    [Fact]
    public async Task AskQuestion_ShouldReturnResult_WhenInformationExists()
    {
        // Arrange
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<AskQuestionTool>();
        
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
        
        // Создаем ChromaDbService
        var chromaLogger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<ChromaDbService>();
        var chromaDbService = new ChromaDbService(httpClient, ollamaService, chromaLogger);
        
        // Создаем RAGConfig
        var ragConfig = Options.Create(new RAGConfig
        {
            MaxChunks = 5,
            MinRelevanceScore = 0.5, // Более лояльный порог
            MaxContextTokens = 2000,
            IncludeMetadataInContext = true,
            GradeDocuments = new GradeDocumentsConfig
            {
                Enabled = false // Отключаем оценку для теста
            }
        });
        
        // Создаем ContextFormatter
        var contextFormatter = new ContextFormatter();
        
        // Создаем RagGraphService
        var ragLogger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<RagGraphService>();
        var ragGraphService = new RagGraphService(
            chromaDbService, 
            ollamaService, 
            ragConfig, 
            contextFormatter, 
            ragLogger);
        
        // Создаем AskQuestionTool
        var askQuestionTool = new AskQuestionTool(ragGraphService, ragConfig, logger);
        
        // Act
        try
        {
            // Вопрос с существующей информацией в docs/test_docs/
            var question = "какая красивая кличка для кошки?";
            var result = await askQuestionTool.AskQuestion(question);
            
            // Assert
            Console.WriteLine($"Вопрос: {question}");
            Console.WriteLine($"Ответ: {result}");
            
            Assert.False(string.IsNullOrEmpty(result));
            Assert.Contains("Моня", result); // Проверка, что ответ содержит кличку из cats.txt
            Assert.DoesNotContain("не найдено информации", result);
            Assert.DoesNotContain("❌", result);
            
            Console.WriteLine("Тест пройден успешно! Ответ содержит ожидаемую информацию.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при тестировании AskQuestionTool: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Внутренняя ошибка: {ex.InnerException.Message}");
            }
            Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
            
            Assert.Fail($"Тест провален: {ex.Message}");
        }
    }

    /// <summary>
    /// Проверяет, что AskQuestionTool выдает отрицательный результат для вопроса без информации в документах.
    /// </summary>
    [Fact]
    public async Task AskQuestion_ShouldReturnNegativeResult_WhenInformationDoesNotExist()
    {
        // Arrange
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<AskQuestionTool>();
        
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
        
        // Создаем ChromaDbService
        var chromaLogger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<ChromaDbService>();
        var chromaDbService = new ChromaDbService(httpClient, ollamaService, chromaLogger);
        
        // Создаем RAGConfig
        var ragConfig = Options.Create(new RAGConfig
        {
            MaxChunks = 5,
            MinRelevanceScore = 0.5, // Более лояльный порог
            MaxContextTokens = 2000,
            IncludeMetadataInContext = true,
            GradeDocuments = new GradeDocumentsConfig
            {
                Enabled = false // Отключаем оценку для теста
            }
        });
        
        // Создаем ContextFormatter
        var contextFormatter = new ContextFormatter();
        
        // Создаем RagGraphService
        var ragLogger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<RagGraphService>();
        var ragGraphService = new RagGraphService(
            chromaDbService, 
            ollamaService, 
            ragConfig, 
            contextFormatter, 
            ragLogger);
        
        // Создаем AskQuestionTool
        var askQuestionTool = new AskQuestionTool(ragGraphService, ragConfig, logger);
        
        // Act
        try
        {
            // Вопрос с несуществующей информацией в docs/test_docs/
            var question = "какая кличка для попугая?";
            var result = await askQuestionTool.AskQuestion(question);
            
            // Assert
            Console.WriteLine($"Вопрос: {question}");
            Console.WriteLine($"Ответ: {result}");
            
            Assert.False(string.IsNullOrEmpty(result));
            Assert.Contains("не найдено информации", result);
            Assert.Contains("❌", result);
            Assert.DoesNotContain("Моня", result);
            Assert.DoesNotContain("Рекс", result);
            
            Console.WriteLine("Тест пройден успешно! Ответ содержит ожидаемое отрицательное сообщение.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при тестировании AskQuestionTool: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Внутренняя ошибка: {ex.InnerException.Message}");
            }
            Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
            
            Assert.Fail($"Тест провален: {ex.Message}");
        }
    }

    /// <summary>
    /// Проверяет, что AskQuestionTool корректно работает с вопросами на английском языке.
    /// </summary>
    [Fact]
    public async Task AskQuestion_ShouldReturnResult_WhenQuestionInEnglish()
    {
        // Arrange
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<AskQuestionTool>();
        
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
        
        // Создаем ChromaDbService
        var chromaLogger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<ChromaDbService>();
        var chromaDbService = new ChromaDbService(httpClient, ollamaService, chromaLogger);
        
        // Создаем RAGConfig
        var ragConfig = Options.Create(new RAGConfig
        {
            MaxChunks = 5,
            MinRelevanceScore = 0.5, // Более лояльный порог
            MaxContextTokens = 2000,
            IncludeMetadataInContext = true,
            GradeDocuments = new GradeDocumentsConfig
            {
                Enabled = false // Отключаем оценку для теста
            }
        });
        
        // Создаем ContextFormatter
        var contextFormatter = new ContextFormatter();
        
        // Создаем RagGraphService
        var ragLogger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<RagGraphService>();
        var ragGraphService = new RagGraphService(
            chromaDbService, 
            ollamaService, 
            ragConfig, 
            contextFormatter, 
            ragLogger);
        
        // Создаем AskQuestionTool
        var askQuestionTool = new AskQuestionTool(ragGraphService, ragConfig, logger);
        
        // Act
        try
        {
            // Вопрос на английском с существующей информацией в test_docs/
            var question = "what's a beautiful name for a dog?";
            var result = await askQuestionTool.AskQuestion(question);
            
            // Assert
            Console.WriteLine($"Вопрос: {question}");
            Console.WriteLine($"Ответ: {result}");
            
            Assert.False(string.IsNullOrEmpty(result));
            Assert.Contains("Рекс", result);
            Assert.DoesNotContain("не найдено информации", result);
            
            Console.WriteLine("Тест пройден успешно! Ответ содержит ожидаемую информацию.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при тестировании AskQuestionTool: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Внутренняя ошибка: {ex.InnerException.Message}");
            }
            Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
            
            Assert.Fail($"Тест провален: {ex.Message}");
        }
    }

    /// <summary>
    /// Проверяет, что AskQuestionTool работает с вопросами о собаках.
    /// </summary>
    [Fact]
    public async Task AskQuestion_ShouldReturnResult_WhenQuestionAboutDogs()
    {
        // Arrange
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<AskQuestionTool>();
        
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
        
        // Создаем ChromaDbService
        var chromaLogger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<ChromaDbService>();
        var chromaDbService = new ChromaDbService(httpClient, ollamaService, chromaLogger);
        
        // Создаем RAGConfig
        var ragConfig = Options.Create(new RAGConfig
        {
            MaxChunks = 5,
            MinRelevanceScore = 0.5, // Более лояльный порог
            MaxContextTokens = 2000,
            IncludeMetadataInContext = true,
            GradeDocuments = new GradeDocumentsConfig
            {
                Enabled = false // Отключаем оценку для теста
            }
        });
        
        // Создаем ContextFormatter
        var contextFormatter = new ContextFormatter();
        
        // Создаем RagGraphService
        var ragLogger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<RagGraphService>();
        var ragGraphService = new RagGraphService(
            chromaDbService, 
            ollamaService, 
            ragConfig, 
            contextFormatter, 
            ragLogger);
        
        // Создаем AskQuestionTool
        var askQuestionTool = new AskQuestionTool(ragGraphService, ragConfig, logger);
        
        // Act
        try
        {
            // Вопрос о собаках
            var question = "что любят собаки?";
            var result = await askQuestionTool.AskQuestion(question);
            
            // Assert
            Console.WriteLine($"Вопрос: {question}");
            Console.WriteLine($"Ответ: {result}");
            
            Assert.False(string.IsNullOrEmpty(result));
            Assert.Contains("резиновых уток", result); // Проверка, что ответ содержит информацию из dogs.txt
            Assert.DoesNotContain("не найдено информации", result);
            
            Console.WriteLine("Тест пройден успешно! Ответ содержит ожидаемую информацию.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при тестировании AskQuestionTool: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Внутренняя ошибка: {ex.InnerException.Message}");
            }
            Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
            
            Assert.Fail($"Тест провален: {ex.Message}");
        }
    }
}