using McpRag;
using McpRag.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace McpRag.Tests;

/// <summary>
/// Тесты для AskQuestionTool - инструмента для получения ответов на вопросы из базы знаний с использованием RAG.
/// Проверяют работу с вопросами, обработку ошибок и формат возвращаемого результата.
/// </summary>
public class AskQuestionToolTests
{
    private readonly Mock<IRagGraphService> _ragGraphMock;
    private readonly AskQuestionTool _askQuestionTool;
    private readonly RAGConfig _config;

    public AskQuestionToolTests()
    {
        _ragGraphMock = new Mock<IRagGraphService>();
        _config = new RAGConfig
        {
            MinRelevanceScore = 0.7f,
            MaxChunks = 5,
            MaxContextTokens = 2000,
            IncludeMetadataInContext = true
        };
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<AskQuestionTool>();
        var configOptions = Options.Create(_config);
        _askQuestionTool = new AskQuestionTool(_ragGraphMock.Object, configOptions, logger);
    }

    /// <summary>
    /// Проверяет, что метод AskQuestion возвращает ответ при отправке валидного вопроса.
    /// Убеждается, что ответ не пустой и содержит ожидаемую информацию.
    /// </summary>
    [Fact]
    public async Task AskQuestion_ShouldReturnResponse_ForValidQuestion()
    {
        // Arrange
        var question = "What is RAG?";
        var mockState = CreateMockRagState(question);
        mockState.Documents.AddRange(new[]
        {
            new DocumentChunk { Source = "test1.txt", Score = 0.8f },
            new DocumentChunk { Source = "test2.txt", Score = 0.9f }
        });
        _ragGraphMock.Setup(x => x.ExecuteAsync(question, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockState);

        // Act
        var result = await _askQuestionTool.AskQuestion(question);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("RAG stands for Retrieval-Augmented Generation", result);
        Assert.Contains("📊 **Путь выполнения:**", result);
        Assert.Contains("📚 **Источники:**", result);
    }

    /// <summary>
    /// Проверяет, что метод AskQuestion возвращает ошибку при возникновении исключения в RAG графе.
    /// Убеждается, что инструмент корректно обрабатывает ошибки и возвращает пользовательское сообщение.
    /// </summary>
    [Fact]
    public async Task AskQuestion_WithRagGraphError_ShouldReturnErrorMessage()
    {
        // Arrange
        var question = "What is AI?";
        var testError = new Exception("Test exception");
        _ragGraphMock.Setup(x => x.ExecuteAsync(question, It.IsAny<CancellationToken>()))
            .ThrowsAsync(testError);

        // Act
        var result = await _askQuestionTool.AskQuestion(question);

        // Assert
        Assert.Contains("❌", result);
        Assert.Contains("Ошибка при обработке вопроса", result);
    }

    /// <summary>
    /// Проверяет, что метод AskQuestion обрабатывает состояние с ошибкой из RAG графа.
    /// Убеждается, что инструмент правильно отображает сообщение об ошибке.
    /// </summary>
    [Fact]
    public async Task AskQuestion_WithErrorState_ShouldReturnErrorMessage()
    {
        // Arrange
        var question = "What is AI?";
        var mockState = CreateMockRagState(question);
        mockState.HasError = true;
        mockState.ErrorMessage = "Test error message";
        _ragGraphMock.Setup(x => x.ExecuteAsync(question, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockState);

        // Act
        var result = await _askQuestionTool.AskQuestion(question);

        // Assert
        Assert.Contains("❌ Ошибка: Test error message", result);
    }

    /// <summary>
    /// Проверяет, что метод AskQuestion обрабатывает пустой вопрос корректно.
    /// Убеждается, что инструмент не выбрасывает исключение и возвращает сообщение об ошибке.
    /// </summary>
    [Fact]
    public async Task AskQuestion_WithEmptyQuestion_ShouldReturnErrorMessage()
    {
        // Arrange
        var question = string.Empty;
        var mockState = CreateMockRagState(question);
        _ragGraphMock.Setup(x => x.ExecuteAsync(question, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockState);

        // Act
        var result = await _askQuestionTool.AskQuestion(question);

        // Assert
        Assert.NotNull(result);
    }

    /// <summary>
    /// Проверяет, что метод AskQuestion обрабатывает null вопрос.
    /// Убеждается, что инструмент не выбрасывает исключение и возвращает сообщение об ошибке.
    /// </summary>
    [Fact]
    public async Task AskQuestion_WithNullQuestion_ShouldReturnErrorMessage()
    {
        // Arrange
        string question = null;
        var mockState = CreateMockRagState(question);
        _ragGraphMock.Setup(x => x.ExecuteAsync(question, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockState);

        // Act
        var result = await _askQuestionTool.AskQuestion(question);

        // Assert
        Assert.NotNull(result);
    }

    /// <summary>
    /// Проверяет, что метод AskQuestion включает информацию о релевантных источниках в ответ.
    /// Убеждается, что в ответе присутствует блок источников с именами файлов.
    /// </summary>
    [Fact]
    public async Task AskQuestion_ShouldIncludeSources_WhenDocumentsFound()
    {
        // Arrange
        var question = "What is RAG?";
        var mockState = CreateMockRagState(question);
        mockState.Documents.AddRange(new[]
        {
            new DocumentChunk { Source = "test1.txt", Score = 0.8f },
            new DocumentChunk { Source = "test2.txt", Score = 0.9f }
        });
        _ragGraphMock.Setup(x => x.ExecuteAsync(question, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockState);

        // Act
        var result = await _askQuestionTool.AskQuestion(question);

        // Assert
        Assert.Contains("📚 **Источники:**", result);
        Assert.Contains("test1.txt", result);
        Assert.Contains("test2.txt", result);
    }

    /// <summary>
    /// Проверяет, что метод AskQuestion включает информацию о пути выполнения в ответ.
    /// Убеждается, что в ответе присутствует блок с шагами выполнения графа.
    /// </summary>
    [Fact]
    public async Task AskQuestion_ShouldIncludeExecutionSteps_WhenStepsExist()
    {
        // Arrange
        var question = "What is RAG?";
        var mockState = CreateMockRagState(question);
        mockState.ExecutionSteps.AddRange(new[]
        {
            new ExecutionStep { NodeName = "InitialQuery" },
            new ExecutionStep { NodeName = "SearchDocuments" },
            new ExecutionStep { NodeName = "GradeDocuments" }
        });
        _ragGraphMock.Setup(x => x.ExecuteAsync(question, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockState);

        // Act
        var result = await _askQuestionTool.AskQuestion(question);

        // Assert
        Assert.Contains("📊 **Путь выполнения:**", result);
        Assert.Contains("InitialQuery", result);
        Assert.Contains("SearchDocuments", result);
        Assert.Contains("GradeDocuments", result);
    }

    /// <summary>
    /// Создает мок объекта RagState для тестирования.
    /// </summary>
    /// <param name="question">Вопрос для тестирования.</param>
    /// <returns>Мок объекта RagState.</returns>
    private RagState CreateMockRagState(string question)
    {
        var config = Options.Create(_config);
        var state = new RagState(config)
        {
            Question = question,
            Answer = "RAG stands for Retrieval-Augmented Generation. It is a technique that combines retrieval of information from external sources with text generation to produce more accurate and contextually relevant answers.",
            CurrentQuery = question,
            QueryHistory = new List<string> { question },
            GroundingScore = 0.85f
        };
        state.ExecutionSteps.AddRange(new[]
        {
            new ExecutionStep { NodeName = "InitialQuery" },
            new ExecutionStep { NodeName = "SearchDocuments" },
            new ExecutionStep { NodeName = "GradeDocuments" }
        });
        return state;
    }
}