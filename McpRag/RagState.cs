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
