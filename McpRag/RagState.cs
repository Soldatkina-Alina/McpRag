using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace McpRag;

/// <summary>
/// Представляет состояние графа RAG.
/// </summary>
public class RagState
{
    private readonly RAGConfig _config;

    /// <summary>
    /// Конструктор состояния RAG.
    /// </summary>
    /// <param name="config">Конфигурация RAG.</param>
    public RagState(IOptions<RAGConfig> config)
    {
        _config = config.Value;
        Documents = new List<DocumentChunk>();
        ExecutionSteps = new List<ExecutionStep>();
        QueryHistory = new List<string>();
        AnswerHistory = new List<string>();
    }

    /// <summary>
    /// Вопрос пользователя.
    /// </summary>
    public string Question { get; set; }

    /// <summary>
    /// Найденные документы.
    /// </summary>
    public List<DocumentChunk> Documents { get; set; }

    /// <summary>
    /// Ответ на вопрос.
    /// </summary>
    public string Answer { get; set; }

    /// <summary>
    /// Шаги выполнения графа.
    /// </summary>
    public List<ExecutionStep> ExecutionSteps { get; set; }

    /// <summary>
    /// Флаг наличия ошибки.
    /// </summary>
    public bool HasError { get; set; }

    /// <summary>
    /// Сообщение об ошибке.
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// Текущий поисковый запрос (может быть перезаписан или расширен).
    /// </summary>
    public string CurrentQuery { get; set; }

    /// <summary>
    /// История запросов (исходный + все варианты).
    /// </summary>
    public List<string> QueryHistory { get; set; }

    /// <summary>
    /// Количество попыток поиска.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Динамический порог релевантности для текущей попытки.
    /// </summary>
    public float CurrentScoreThreshold { get; set; }

    /// <summary>
    /// Флаг, указывающий, основан ли ответ только на контексте.
    /// </summary>
    public bool IsGrounded { get; set; }

    /// <summary>
    /// Количество регенераций ответа.
    /// </summary>
    public int RegenerationCount { get; set; }

    /// <summary>
    /// История ответов (все варианты).
    /// </summary>
    public List<string> AnswerHistory { get; set; }

    /// <summary>
    /// Оценка достоверности (0-1), насколько ответ основан на контексте.
    /// </summary>
    public float? GroundingScore { get; set; }

    /// <summary>
    /// Проверяет, есть ли релевантные документы.
    /// </summary>
    public bool HasRelevantDocuments =>
        Documents.Any(d => d.IsRelevant && d.Score >= _config.MinRelevanceScore);

    /// <summary>
    /// Количество релевантных документов.
    /// </summary>
    public int RelevantCount =>
        Documents.Count(d => d.IsRelevant && d.Score >= _config.MinRelevanceScore);
}
