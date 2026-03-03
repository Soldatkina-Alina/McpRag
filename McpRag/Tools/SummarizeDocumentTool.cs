using McpRag;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;

namespace McpRag.Tools;

/// <summary>
/// Инструмент для создания краткого содержания документа.
/// </summary>
public class SummarizeDocumentTool
{
    private readonly IIndexerService _indexer;
    private readonly IOllamaService _ollama;
    private readonly ILogger<SummarizeDocumentTool> _logger;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="SummarizeDocumentTool"/>.
    /// </summary>
    /// <param name="indexer">Сервис для индексации документов.</param>
    /// <param name="ollama">Сервис для работы с Ollama.</param>
    /// <param name="logger">Логгер для записи информации о работе инструмента.</param>
    public SummarizeDocumentTool(
        IIndexerService indexer,
        IOllamaService ollama,
        ILogger<SummarizeDocumentTool> logger)
    {
        _indexer = indexer ?? throw new ArgumentNullException(nameof(indexer));
        _ollama = ollama ?? throw new ArgumentNullException(nameof(ollama));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Создает краткое содержание документа.
    /// </summary>
    /// <param name="filePath">Путь к файлу для суммаризации.</param>
    /// <param name="maxWords">Максимальная длина саммари (в словах).</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Краткое содержание документа.</returns>
    [McpServerTool]
    [Description("Создает краткое содержание документа")]
    public async Task<string> SummarizeDocument(
        [Description("Путь к файлу для суммаризации")] string filePath,
        [Description("Максимальная длина саммари (в словах)")] int? maxWords = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return $"❌ Файл '{filePath}' не существует.";
            }

            // Используем IndexerService для загрузки и разбиения документа
            var chunks = await _indexer.LoadAndSplitDocumentAsync(filePath, cancellationToken);
            var fullText = string.Join("\n\n", chunks.Select(c => c.Text));

            // Базовая статистика
            if (!chunks.Any())
            {
                var fileInfo = new FileInfo(filePath);
                var emptyResponse = new StringBuilder();
                emptyResponse.AppendLine($"📄 **Саммари документа: {fileInfo.Name}**");
                emptyResponse.AppendLine();
                emptyResponse.AppendLine("❌ Документ пустой или не содержит содержимого для суммаризации.");
                emptyResponse.AppendLine();
                emptyResponse.AppendLine("---");
                emptyResponse.AppendLine($"📊 **Статистика:**");
                emptyResponse.AppendLine($"- Размер файла: {fileInfo.Length / 1024.0:F1} KB");
                emptyResponse.AppendLine($"- Чанков: 0");
                emptyResponse.AppendLine($"- Слова: 0");
                return emptyResponse.ToString();
            }

            var stats = new
            {
                FileName = Path.GetFileName(filePath),
                Size = new FileInfo(filePath).Length,
                Chunks = chunks.Count,
                Words = fullText.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length
            };

            // Определяем длину саммари
            var wordLimit = maxWords ?? Math.Max(50, stats.Words / 10);

            var prompt = $@"
Ты - эксперт по суммаризации документов.
Создай краткое содержание следующего документа.

Требования:
- Максимум {wordLimit} слов
- Выдели основные темы и ключевые моменты
- Сохрани важные детали
- Напиши на том же языке, что и документ

Документ:
{fullText.Substring(0, Math.Min(5000, fullText.Length))}
{(fullText.Length > 5000 ? "\n...[документ обрезан для суммаризации]" : "")}

Краткое содержание:";

            var summary = await _ollama.GenerateAsync(prompt, cancellationToken);

            var response = new StringBuilder();
            response.AppendLine($"📄 **Саммари документа: {stats.FileName}**");
            response.AppendLine();
            response.AppendLine(summary);
            response.AppendLine();
            response.AppendLine("---");
            response.AppendLine($"📊 **Статистика:**");
            response.AppendLine($"- Размер файла: {stats.Size / 1024.0:F1} KB");
            response.AppendLine($"- Всего слов: {stats.Words}");
            response.AppendLine($"- Чанков: {stats.Chunks}");
            response.AppendLine($"- Длина саммари: {summary.Split().Length} слов");

            return response.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SummarizeDocument");
            return $"❌ Ошибка: {ex.Message}";
        }
    }
}