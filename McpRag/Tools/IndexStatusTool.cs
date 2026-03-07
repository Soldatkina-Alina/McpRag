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
/// Инструмент для отображения статуса индекса.
/// </summary>
public class IndexStatusTool
{
    private readonly IVectorStoreService _vectorStore;
    private readonly ILogger<IndexStatusTool> _logger;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="IndexStatusTool"/>.
    /// </summary>
    /// <param name="vectorStore">Сервис для работы с векторным хранилищем.</param>
    /// <param name="logger">Логгер для записи информации о работе инструмента.</param>
    public IndexStatusTool(
        IVectorStoreService vectorStore,
        ILogger<IndexStatusTool> logger)
    {
        _vectorStore = vectorStore ?? throw new ArgumentNullException(nameof(vectorStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Показывает статистику индекса.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Строка с статистикой индекса.</returns>
    [McpServerTool]
    [Description("Показывает статистику индекса")]
    public async Task<string> IndexStatus(CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _vectorStore.GetStatisticsAsync(cancellationToken);

            var response = new StringBuilder();
            response.AppendLine("📊 **Статус индекса:**");
            response.AppendLine();

            if (stats.TotalChunks == 0)
            {
                response.AppendLine("❌ Индекс пуст. Выполните `index_folder` для индексации документов.");
                return response.ToString();
            }

            response.AppendLine($"📁 **Всего файлов:** {stats.TotalFiles}");
            response.AppendLine($"📄 **Всего чанков:** {stats.TotalChunks}");

            if (stats.LastIndexed.HasValue)
            {
                response.AppendLine($"🕒 **Последняя индексация:** {stats.LastIndexed.Value:yyyy-MM-dd HH:mm:ss}");
                response.AppendLine($"⏱️ **Прошло:** {(DateTime.UtcNow - stats.LastIndexed.Value).TotalHours:F1} часов");
            }

            // Дополнительная информация, если доступна
            if (stats.Collections != null && stats.Collections.Any())
            {
                response.AppendLine();
                response.AppendLine("🗂️ **Коллекции ChromaDB:**");
                foreach (var collection in stats.Collections)
                {
                    response.AppendLine($"- {collection.Name}: {collection.Count} документов");
                }
            }

            return response.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in IndexStatus");
            return $"❌ Ошибка при получении статуса: {ex.Message}";
        }
    }
}