using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpRag;

/// <summary>
/// Сервис для выполнения графа RAG.
/// </summary>
public class RagGraphService : IRagGraphService
{
    private readonly IVectorStoreService _vectorStore;
    private readonly IOllamaService _ollama;
    private readonly IOptions<RAGConfig> _config;
    private readonly ContextFormatter _contextFormatter;
    private readonly ILogger<RagGraphService> _logger;

    /// <summary>
    /// Конструктор сервиса графа RAG.
    /// </summary>
    /// <param name="vectorStore">Сервис векторного хранилища.</param>
    /// <param name="ollama">Сервис Ollama.</param>
    /// <param name="config">Конфигурация RAG.</param>
    /// <param name="contextFormatter">Форматировщик контекста.</param>
    /// <param name="logger">Логгер.</param>
    public RagGraphService(
        IVectorStoreService vectorStore,
        IOllamaService ollama,
        IOptions<RAGConfig> config,
        ContextFormatter contextFormatter,
        ILogger<RagGraphService> logger)
    {
        _vectorStore = vectorStore;
        _ollama = ollama;
        _config = config;
        _contextFormatter = contextFormatter;
        _logger = logger;
    }

    /// <summary>
    /// Выполняет график RAG.
    /// </summary>
    /// <param name="question">Вопрос пользователя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Состояние графа RAG.</returns>
    public async Task<RagState> ExecuteAsync(string question, CancellationToken ct)
    {
        var state = new RagState(_config) { Question = question };
        
        try
        {
            // Узел 1: Поиск документов
            state = await SearchNodeAsync(state, ct);
            
            // Проверка релевантности
            if (!state.HasRelevantDocuments || state.Documents.Count == 0 || state.Documents.All(d => d.Score < _config.Value.MinRelevanceScore))
            {
                var maxScore = state.Documents.FirstOrDefault()?.Score ?? 0;
                state.Answer = $"❌ В предоставленных документах не найдено информации.\n" +
                               $"Наиболее похожие документы имеют релевантность {maxScore:P1}, " +
                               $"что ниже порога {_config.Value.MinRelevanceScore:P1}.";
                
                state.ExecutionSteps.Add(new ExecutionStep 
                { 
                    NodeName = "NoRelevantDocs",
                    Metadata = new Dictionary<string, object>
                    {
                        ["max_score"] = maxScore,
                        ["threshold"] = _config.Value.MinRelevanceScore,
                        ["total_chunks"] = state.Documents.Count
                    }
                });
                return state;
            }
            
            // Узел 2: Генерация ответа
            state = await GenerateNodeAsync(state, ct);
            
            return state;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при выполнении графа RAG");
            state.HasError = true;
            state.ErrorMessage = ex.Message;
            state.ExecutionSteps.Add(new ExecutionStep 
            { 
                NodeName = "Error",
                Metadata = new Dictionary<string, object> { ["error"] = ex.Message }
            });
            return state;
        }
    }

    /// <summary>
    /// Узел графа для поиска документов.
    /// </summary>
    /// <param name="state">Состояние графа.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Обновленное состояние графа.</returns>
    private async Task<RagState> SearchNodeAsync(RagState state, CancellationToken ct)
    {
        var step = new ExecutionStep { NodeName = "Search" };
        
        try
        {
            var results = await _vectorStore.SearchWithScoreAsync(
                state.Question, 
                _config.Value.MaxChunks, 
                ct);
            
            state.Documents = results.Select(r => 
            {
                r.Chunk.Score = r.Score;
                return r.Chunk;
            }).ToList();
            
            step.Metadata["chunks_found"] = state.Documents.Count;
            step.Metadata["max_score"] = state.Documents.FirstOrDefault()?.Score ?? 0;
            step.Metadata["threshold"] = _config.Value.MinRelevanceScore;
        }
        catch (Exception ex)
        {
            step.Metadata["error"] = ex.Message;
            throw;
        }
        finally
        {
            state.ExecutionSteps.Add(step);
        }
        
        return state;
    }

    /// <summary>
    /// Узел графа для генерации ответа.
    /// </summary>
    /// <param name="state">Состояние графа.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Обновленное состояние графа.</returns>
    private async Task<RagState> GenerateNodeAsync(RagState state, CancellationToken ct)
    {
        var step = new ExecutionStep { NodeName = "Generate" };
        
        try
        {
            var relevantChunks = state.Documents
                .Where(d => d.Score >= _config.Value.MinRelevanceScore)
                .ToList();
            
            var context = _contextFormatter.FormatContext(
                relevantChunks, 
                _config.Value.IncludeMetadataInContext);
            
            if (context.Length > _config.Value.MaxContextTokens * 4)
            {
                context = context.Substring(0, _config.Value.MaxContextTokens * 4) + 
                         "\n\n...[контекст обрезан]";
                step.Metadata["truncated"] = true;
            }
            
            var prompt = $@"
Ты - ассистент, который отвечает на вопросы, используя ТОЛЬКО информацию из предоставленного контекста.

ВАЖНЫЕ ПРАВИЛА:
1. Отвечай строго на основе контекста, не используй свои знания
2. Если в контексте нет ответа - скажи, что информации нет
3. Ссылайся на источники в формате [Источник N]
4. Не придумывай факты и не дополняй информацию
5. Если информация неполная - так и скажи

Контекст (документы):
{context}

Вопрос пользователя: {state.Question}

Ответ (только на основе контекста, с указанием источников):";
            
            state.Answer = await _ollama.GenerateAsync(prompt, ct);
            
            step.Metadata["chunks_used"] = relevantChunks.Count;
            step.Metadata["context_length"] = context.Length;
            step.Metadata["truncated"] = context.Length > _config.Value.MaxContextTokens * 4;
        }
        catch (Exception ex)
        {
            step.Metadata["error"] = ex.Message;
            throw;
        }
        finally
        {
            state.ExecutionSteps.Add(step);
        }
        
        return state;
    }
}