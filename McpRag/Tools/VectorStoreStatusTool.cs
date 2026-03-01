using System.ComponentModel;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace McpRag.Tools;

/// <summary>
/// Tool for checking vector store status.
/// </summary>
internal class VectorStoreStatusTool
{
    private readonly IVectorStoreService _vectorStore;
    private readonly ILogger<VectorStoreStatusTool> _logger;

    public VectorStoreStatusTool(IVectorStoreService vectorStore, ILogger<VectorStoreStatusTool> logger)
    {
        _vectorStore = vectorStore;
        _logger = logger;
    }

    [McpServerTool]
    [Description("Checks vector store status and statistics.")]
    public async Task<string> VectorStoreStatus(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking vector store status");

            var count = await _vectorStore.CountAsync(cancellationToken);

            var response = new System.Text.StringBuilder();
            response.AppendLine("✅ ChromaDB статус:");
            response.AppendLine($"- Коллекция: documents");
            response.AppendLine($"- Адрес сервера: http://localhost:8000");
            response.AppendLine($"- Количество документов: {count}");
            
            if (count > 0)
            {
                response.AppendLine($"- Статус: ✅ Активен и содержит данные");
            }
            else
            {
                response.AppendLine($"- Статус: ⚠️ Пустой - индексируйте папку с документами");
            }

            return response.ToString();
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error checking vector store status: {Message}", ex.Message);
            return $"❌ Ошибка при проверке статуса векторного хранилища: {ex.Message}";
        }
    }

    [McpServerTool]
    [Description("Clears all documents from vector store.")]
    public async Task<string> ClearVectorStore(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Clearing vector store");

            await _vectorStore.ClearAsync(cancellationToken);

            return "✅ Векторное хранилище успешно очищено";
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error clearing vector store: {Message}", ex.Message);
            return $"❌ Ошибка при очистке векторного хранилища: {ex.Message}";
        }
    }
}