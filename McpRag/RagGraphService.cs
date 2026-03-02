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
        var state = new RagState(_config) 
        { 
            Question = question,
            CurrentQuery = question,
            CurrentScoreThreshold = _config.Value.MinRelevanceScore
        };
        
        try
        {
            // Шаг 1: Rewrite (улучшаем запрос перед первым поиском)
            if (_config.Value.Retry.EnableQueryRewrite)
            {
                state = await RewriteQueryNodeAsync(state, ct);
            }
            
            // Начало retry-цикла
            while (state.RetryCount <= _config.Value.Retry.MaxRetries)
            {
                // Шаг 2: Search с текущим запросом
                state = await SearchNodeAsync(state, ct);
                
                // Шаг 3: GradeDocuments (из Задачи 10)
                state = await GradeDocumentsNodeAsync(state, ct);
                
                // Проверка: достаточно ли документов?
                if (HasEnoughRelevantDocuments(state))
                {
                    break; // достаточно - выходим из цикла
                }
                
                // Если есть еще попытки - расширяем запрос
                if (state.RetryCount < _config.Value.Retry.MaxRetries)
                {
                    state = await BroadenQueryNodeAsync(state, ct);
                    // Продолжаем цикл
                }
                else
                {
                    // Достигнут лимит попыток
                    state.ExecutionSteps.Add(new ExecutionStep 
                    { 
                        NodeName = "MaxRetriesReached",
                        Metadata = new Dictionary<string, object>
                        {
                            ["max_retries"] = _config.Value.Retry.MaxRetries,
                            ["relevant_found"] = state.Documents.Count(d => d.IsRelevant)
                        }
                    });
                    break;
                }
            }
            
            // Если после всех попыток нет релевантных документов
            if (!state.Documents.Any(d => d.IsRelevant))
            {
                state.Answer = $"❌ После {state.RetryCount + 1} попыток не найдено релевантных документов.\n" +
                               $"Последний запрос: '{state.CurrentQuery}'\n" +
                               $"История запросов: {string.Join(" → ", state.QueryHistory)}";
                return state;
            }
            
            // Шаг 4: Generate (из Задачи 9)
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
    /// Узел графа для перезаписи запроса.
    /// </summary>
    /// <param name="state">Состояние графа.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Обновленное состояние графа.</returns>
    private async Task<RagState> RewriteQueryNodeAsync(RagState state, CancellationToken ct)
    {
        var step = new ExecutionStep { NodeName = "RewriteQuery" };
        
        try
        {
            var prompt = $@"
Ты - эксперт по улучшению поисковых запросов для RAG системы.
Исходный запрос пользователя: {state.Question}

Твоя задача: перепиши запрос так, чтобы он лучше подходил для поиска релевантных документов.
- Используй более точные термины
- Добавь важные ключевые слова
- Сохрани исходный смысл
- Ответь ТОЛЬКО улучшенным запросом, без пояснений

Улучшенный запрос:";
            
            var rewritten = await _ollama.GenerateAsync(prompt, ct);
            state.CurrentQuery = rewritten.Trim();
            state.QueryHistory.Add(rewritten.Trim());
            
            step.Metadata["original"] = state.Question;
            step.Metadata["rewritten"] = state.CurrentQuery;
        }
        catch (Exception ex)
        {
            step.Metadata["error"] = ex.Message;
            state.CurrentQuery = state.Question; // fallback к исходному
        }
        finally
        {
            state.ExecutionSteps.Add(step);
        }
        
        return state;
    }

    /// <summary>
    /// Узел графа для расширения запроса.
    /// </summary>
    /// <param name="state">Состояние графа.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Обновленное состояние графа.</returns>
    private async Task<RagState> BroadenQueryNodeAsync(RagState state, CancellationToken ct)
    {
        var step = new ExecutionStep { NodeName = "BroadenQuery" };
        
        try
        {
            var prompt = $@"
Ты - эксперт по расширению поисковых запросов.
Текущий запрос: {state.CurrentQuery}

Найдено недостаточно релевантных документов.
Расширь запрос, чтобы найти больше информации:
- Добавь синонимы
- Используй более общие термины
- Включи связанные понятия

Ответь ТОЛЬКО расширенным запросом, без пояснений.

Расширенный запрос:";
            
            var broadened = await _ollama.GenerateAsync(prompt, ct);
            state.CurrentQuery = broadened.Trim();
            state.QueryHistory.Add(broadened.Trim());
            state.RetryCount++;
            
            // Динамически снижаем порог релевантности при каждой попытке
            state.CurrentScoreThreshold = _config.Value.MinRelevanceScore * 
                                         (1 - _config.Value.Retry.ScoreBoostPerRetry * state.RetryCount);
            
            step.Metadata["previous_query"] = state.QueryHistory[^2];
            step.Metadata["broadened_query"] = state.CurrentQuery;
            step.Metadata["retry_count"] = state.RetryCount;
            step.Metadata["new_threshold"] = state.CurrentScoreThreshold;
        }
        catch (Exception ex)
        {
            step.Metadata["error"] = ex.Message;
            // при ошибке оставляем тот же запрос
        }
        finally
        {
            state.ExecutionSteps.Add(step);
        }
        
        return state;
    }

    /// <summary>
    /// Проверяет, достаточно ли релевантных документов.
    /// </summary>
    /// <param name="state">Состояние графа.</param>
    /// <returns>True, если количество релевантных документов достаточно.</returns>
    private bool HasEnoughRelevantDocuments(RagState state)
    {
        var config = _config.Value.Retry;
        
        // Используем динамический порог и счетчик релевантных
        var relevantCount = state.Documents.Count(d => 
            d.IsRelevant && d.Score >= state.CurrentScoreThreshold);
        
        return relevantCount >= config.MinRelevantCount;
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
                state.CurrentQuery ?? state.Question, 
                _config.Value.MaxChunks, 
                ct);
            
            state.Documents = results.Select(r => 
            {
                r.Chunk.Score = r.Score;
                return r.Chunk;
            }).ToList();
            
            step.Metadata["query_used"] = state.CurrentQuery;
            step.Metadata["chunks_found"] = state.Documents.Count;
            step.Metadata["retry_count"] = state.RetryCount;
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
    /// Узел графа для оценки релевантности документов через LLM.
    /// </summary>
    /// <param name="state">Состояние графа.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Обновленное состояние графа.</returns>
    private async Task<RagState> GradeDocumentsNodeAsync(RagState state, CancellationToken ct)
    {
        var step = new ExecutionStep { NodeName = "GradeDocuments" };
        var config = _config.Value.GradeDocuments;
        
        // Если узел отключен - просто возвращаем state
        if (!config.Enabled)
        {
            step.Metadata["skipped"] = true;
            state.ExecutionSteps.Add(step);
            return state;
        }
        
        var documentsToGrade = new List<DocumentChunk>();
        var skippedDocs = new List<DocumentChunk>();
        var errors = 0;
        
        try
        {
            // Разделяем документы на те, что нужно оценить, и те, что можно пропустить
            foreach (var doc in state.Documents)
            {
                // Если документ уже имеет высокий Score от ChromaDB - пропускаем оценку
                if (doc.Score >= config.ScoreThreshold)
                {
                    doc.IsRelevant = true;
                    doc.LLMScore = 1.0f; // считаем, что LLM бы тоже сказала "yes"
                    skippedDocs.Add(doc);
                }
                else
                {
                    documentsToGrade.Add(doc);
                }
            }
            
            step.Metadata["total_docs"] = state.Documents.Count;
            step.Metadata["skipped_docs"] = skippedDocs.Count;
            step.Metadata["to_grade"] = documentsToGrade.Count;
            
            // Оцениваем документы, которые требуют проверки
            if (documentsToGrade.Any())
            {
                if (config.BatchSize > 1)
                {
                    await GradeDocumentsBatchAsync(documentsToGrade, state.Question, config, ct);
                }
                else
                {
                    await GradeDocumentsSequentialAsync(documentsToGrade, state.Question, config, ct);
                }
                
                // Собираем статистику по ошибкам
                errors = documentsToGrade.Count(d => !string.IsNullOrEmpty(d.GradeError));
                
                // Обновляем IsRelevant на основе LLMScore
                foreach (var doc in documentsToGrade)
                {
                    doc.IsRelevant = doc.LLMScore >= config.LLMThreshold;
                }
            }
            
            // Формируем финальный список документов (только релевантные)
            state.Documents = state.Documents
                .Where(d => d.IsRelevant && d.Score >= _config.Value.MinRelevanceScore)
                .ToList();
            
            step.Metadata["relevant_after_grade"] = state.Documents.Count;
            step.Metadata["irrelevant"] = documentsToGrade.Count(d => !d.IsRelevant);
            step.Metadata["grade_errors"] = errors;
        }
        catch (Exception ex)
        {
            step.Metadata["error"] = ex.Message;
            _logger.LogError(ex, "GradeDocuments node failed");
            
            // При критической ошибке сохраняем все документы
            state.Documents = state.Documents
                .Where(d => d.Score >= _config.Value.MinRelevanceScore)
                .ToList();
            step.Metadata["fallback_to_score"] = true;
        }
        finally
        {
            state.ExecutionSteps.Add(step);
        }
        
        return state;
    }

    /// <summary>
    /// Последовательная оценка документов.
    /// </summary>
    /// <param name="docs">Список документов для оценки.</param>
    /// <param name="question">Вопрос пользователя.</param>
    /// <param name="config">Конфигурация оценки.</param>
    /// <param name="ct">Токен отмены.</param>
    private async Task GradeDocumentsSequentialAsync(
        List<DocumentChunk> docs, 
        string question, 
        GradeDocumentsConfig config,
        CancellationToken ct)
    {
        foreach (var doc in docs)
        {
            try
            {
                doc.LLMScore = await GradeSingleDocumentAsync(doc, question, config, ct);
                doc.IsRelevant = doc.LLMScore >= config.LLMThreshold;
            }
            catch (Exception ex)
            {
                doc.GradeError = ex.Message;
                doc.LLMScore = null;
                doc.IsRelevant = true; // сохраняем при ошибке
                _logger.LogWarning(ex, "Failed to grade document {docId}", doc.Id);
            }
        }
    }

    /// <summary>
    /// Оценка одного документа.
    /// </summary>
    /// <param name="doc">Документ для оценки.</param>
    /// <param name="question">Вопрос пользователя.</param>
    /// <param name="config">Конфигурация оценки.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Оценка от LLM.</returns>
    private async Task<float> GradeSingleDocumentAsync(
        DocumentChunk doc, 
        string question, 
        GradeDocumentsConfig config,
        CancellationToken ct)
    {
        string prompt;
        
        if (config.UseBinaryScore)
        {
            // Бинарная оценка (yes/no) - проще и быстрее
            prompt = $@"
Ты - эксперт по оценке релевантности документов для RAG системы.
Оцени, содержит ли документ информацию, полезную для ответа на вопрос.

Вопрос: {question}

Документ:
{doc.Text}

Инструкция: Ответь ТОЛЬКО одним словом: 'yes' если документ релевантен, 'no' если нет.
Не добавляй никаких пояснений, только 'yes' или 'no'.";
        }
        else
        {
            // Оценка по шкале 0-1 (более тонкая)
            prompt = $@"
Ты - эксперт по оценке релевантности документов для RAG системы.
Оцени по шкале от 0 до 1, насколько документ релевантен для ответа на вопрос.

Вопрос: {question}

Документ:
{doc.Text}

Инструкция: Ответь ТОЛЬКО числом от 0 до 1, где 0 = полностью нерелевантен, 1 = идеально релевантен.
Не добавляй никаких пояснений, только число.";
        }
        
        var response = await _ollama.GenerateAsync(prompt, ct);
        
        if (config.UseBinaryScore)
        {
            // Надежная обработка бинарного ответа
            var cleanResponse = response.Trim().ToLower();
            if (cleanResponse.StartsWith("y")) return 1.0f; // yes, yeap, yup
            if (cleanResponse.StartsWith("n")) return 0.0f; // no, nope
            
            // Если LLM вернула мусор - логируем и возвращаем 0.5 (нейтрально)
            _logger.LogWarning("Unexpected binary response: {response}", response);
            return 0.5f;
        }
        else
        {
            // Парсинг числа от 0 до 1
            if (float.TryParse(response.Trim(), out var score))
            {
                return Math.Clamp(score, 0, 1);
            }
            
            _logger.LogWarning("Expected float but got: {response}", response);
            return 0.5f; // нейтральное значение при ошибке
        }
    }

    /// <summary>
    /// Пакетная оценка документов.
    /// </summary>
    /// <param name="docs">Список документов для оценки.</param>
    /// <param name="question">Вопрос пользователя.</param>
    /// <param name="config">Конфигурация оценки.</param>
    /// <param name="ct">Токен отмены.</param>
    private async Task GradeDocumentsBatchAsync(
        List<DocumentChunk> docs, 
        string question, 
        GradeDocumentsConfig config,
        CancellationToken ct)
    {
        // Разбиваем на батчи по config.BatchSize
        for (int i = 0; i < docs.Count; i += config.BatchSize)
        {
            var batch = docs.Skip(i).Take(config.BatchSize).ToList();
            
            var prompt = $@"
Ты - эксперт по оценке релевантности документов.
Оцени каждый документ для вопроса: {question}

Документы:
{string.Join("\n---\n", batch.Select((d, idx) => $"[{idx}] {d.Text}"))}

Инструкция: Ответь списком чисел от 0 до 1 через запятую.
Пример: 0.9, 0.2, 0.7";

            var response = await _ollama.GenerateAsync(prompt, ct);
            
            // Парсим ответ (упрощенно)
            var scores = response.Split(',')
                .Select(s => float.TryParse(s.Trim(), out var f) ? f : 0.5f)
                .ToList();
                
            for (int j = 0; j < batch.Count && j < scores.Count; j++)
            {
                batch[j].LLMScore = scores[j];
                batch[j].IsRelevant = scores[j] >= config.LLMThreshold;
            }
        }
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