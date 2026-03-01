using System.ComponentModel;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace McpRag.Tools;

/// <summary>
/// Tool for searching relevant document chunks.
/// </summary>
public class SearchDocsTool
{
    private readonly IVectorStoreService _vectorStore;
    private readonly ILogger<SearchDocsTool> _logger;

    public SearchDocsTool(IVectorStoreService vectorStore, ILogger<SearchDocsTool> logger)
    {
        _vectorStore = vectorStore;
        _logger = logger;
    }

    [McpServerTool]
    [Description("Searches for relevant document chunks based on query.")]
    public async Task<string> SearchDocs(
        [Description("Search query")] string query,
        [Description("Maximum number of results to return")] int topK = 5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Searching for relevant documents with query: {Query}, topK: {TopK}", query, topK);

            // Validate topK value
            if (topK <= 0)
            {
                topK = 5;
            }

            var count = await _vectorStore.CountAsync(cancellationToken);
            if (count == 0)
            {
                return "❌ Векторное хранилище пустое. Сначала индексируйте папку с документами.";
            }

            var results = await _vectorStore.SearchAsync(query, topK, cancellationToken);
            var resultsList = results.ToList();

            if (!resultsList.Any())
            {
                return "❌ Не найдено релевантных документов по запросу.";
            }

            var response = new System.Text.StringBuilder();
            response.AppendLine($"✅ Найдено {resultsList.Count} релевантных документов:");
            response.AppendLine();

            for (int i = 0; i < resultsList.Count; i++)
            {
                var chunk = resultsList[i];
                var fileName = System.IO.Path.GetFileName(chunk.Source);
                
                response.AppendLine($"--- [Источник {i + 1}]: {fileName} (Чанк {chunk.ChunkIndex}) ---");
                response.AppendLine(chunk.Text.Length > 200 ? chunk.Text.Substring(0, 200) + "..." : chunk.Text);
                response.AppendLine();
            }

            return response.ToString();
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error in SearchDocs tool: {Message}", ex.Message);
            return $"❌ Ошибка при поиске документов: {ex.Message}";
        }
    }
}