using McpRag;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;

namespace McpRag.Tools;

/// <summary>
/// Инструмент для поиска релевантных документов по запросу без генерации ответа.
/// </summary>
public class FindRelevantDocsTool
{
    private readonly IVectorStoreService _vectorStore;
    private readonly RAGConfig _config;
    private readonly ILogger<FindRelevantDocsTool> _logger;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="FindRelevantDocsTool"/>.
    /// </summary>
    /// <param name="vectorStore">Сервис для работы с векторным хранилищем.</param>
    /// <param name="config">Конфигурация RAG системы.</param>
    /// <param name="logger">Логгер для записи информации о работе инструмента.</param>
    public FindRelevantDocsTool(
        IVectorStoreService vectorStore,
        IOptions<RAGConfig> config,
        ILogger<FindRelevantDocsTool> logger)
    {
        _vectorStore = vectorStore ?? throw new ArgumentNullException(nameof(vectorStore));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Находит релевантные документы по запросу без генерации ответа.
    /// </summary>
    /// <param name="query">Поисковый запрос.</param>
    /// <param name="topK">Количество результатов (по умолчанию 2).</param>
    /// <param name="minScore">Минимальная релевантность (0-1).</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Строка с найденными релевантными документами.</returns>
    [McpServerTool]
    [Description("Находит релевантные документы по запросу без генерации ответа")]
    public async Task<string> FindRelevantDocs(
        [Description("Поисковый запрос")] string query,
        [Description("Количество результатов (по умолчанию 2)")] int topK = 2,
        [Description("Минимальная релевантность (0-1)")] double? minScore = 0.5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actualMinScore = minScore ?? _config.MinRelevanceScore;

            // Поиск с релевантностью
            var results = await _vectorStore.SearchWithScoreAsync(query, topK, cancellationToken);

            if (!results.Any())
            {
                return "❌ Документы не найдены.";
            }

            // Фильтруем по порогу
            var relevantResults = results.Where(r => r.Score >= actualMinScore).ToList();

            if (!relevantResults.Any())
            {
                var maxScore = results.First().Score;
                return $"❌ Найдено {results.Count} документов, но все ниже порога {actualMinScore:P1}.\n" +
                       $"Максимальная релевантность: {maxScore:P1}";
            }

            var response = new StringBuilder();
            response.AppendLine($"📋 **Найдено {relevantResults.Count} релевантных документов:**");
            response.AppendLine();

            for (int i = 0; i < relevantResults.Count; i++)
            {
                var r = relevantResults[i];
                var chunk = r.Chunk;

                response.AppendLine($"**{i + 1}. {System.IO.Path.GetFileName(chunk.Source)}** " +
                                   $"(релевантность: {r.Score:P1}, чанк {chunk.ChunkIndex})");
                response.AppendLine($"```\n{chunk.Text.Substring(0, Math.Min(300, chunk.Text.Length))}...\n```");
                response.AppendLine();
            }

            return response.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in FindRelevantDocs");
            return $"❌ Ошибка: {ex.Message}";
        }
    }
}
