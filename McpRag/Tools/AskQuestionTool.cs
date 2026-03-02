using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace McpRag.Tools
{
    /// <summary>
    /// Инструмент для получения ответов на вопросы из базы знаний с использованием RAG.
    /// </summary>
    internal class AskQuestionTool
    {
        private readonly IVectorStoreService _vectorStore;
        private readonly IOllamaService _ollama;
        private readonly RAGConfig _config;
        private readonly ContextFormatter _contextFormatter;
        private readonly ILogger<AskQuestionTool> _logger;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="AskQuestionTool"/>.
        /// </summary>
        /// <param name="vectorStore">Сервис для работы с векторным хранилищем.</param>
        /// <param name="ollama">Сервис для работы с Ollama.</param>
        /// <param name="config">Конфигурация RAG.</param>
        /// <param name="contextFormatter">Форматтер контекста.</param>
        /// <param name="logger">Логгер.</param>
        public AskQuestionTool(
            IVectorStoreService vectorStore,
            IOllamaService ollama,
            IOptions<RAGConfig> config,
            ContextFormatter contextFormatter,
            ILogger<AskQuestionTool> logger)
        {
            _vectorStore = vectorStore ?? throw new ArgumentNullException(nameof(vectorStore));
            _ollama = ollama ?? throw new ArgumentNullException(nameof(ollama));
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
            _contextFormatter = contextFormatter ?? throw new ArgumentNullException(nameof(contextFormatter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Получает ответ на вопрос из базы знаний.
        /// </summary>
        /// <param name="question">Вопрос к базе знаний.</param>
        /// <param name="minScore">Минимальная релевантность (0-1).</param>
        /// <param name="maxChunks">Максимальное количество чанков.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Ответ на вопрос.</returns>
        [McpServerTool]
        [Description("Получает ответ на вопрос из базы знаний с использованием RAG")]
        public async Task<string> AskQuestion(
            [Description("Вопрос к базе знаний")] string question,
            [Description("Минимальная релевантность (0-1)")] double? minScore = null,
            [Description("Максимальное количество чанков")] int? maxChunks = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. Получаем настройки
                var actualMinScore = minScore ?? _config.MinRelevanceScore;
                var actualMaxChunks = maxChunks ?? _config.MaxChunks;

                // 2. Поиск релевантных чанков
                var results = await _vectorStore.SearchWithScoreAsync(question, actualMaxChunks, cancellationToken);
                var chunksList = results.ToList();

                // 3. Проверка порога релевантности
                var relevantChunks = chunksList
                    .Where(c => c.Score >= actualMinScore)
                    .Select(c => c.Chunk)
                    .ToList();

                if (!relevantChunks.Any())
                {
                    var maxScore = chunksList.FirstOrDefault()?.Score ?? 0;
                    return "❌ В предоставленных документах не найдено информации по вашему вопросу.\n\n" +
                           $"Наиболее похожие документы имеют релевантность {maxScore:P1}, " +
                           $"что ниже порога {actualMinScore:P1}.";
                }

                // 4. Форматирование контекста
                var context = _contextFormatter.FormatContext(relevantChunks);

                // 5. Формирование промпта для LLM
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

Вопрос пользователя: {question}

Ответ (только на основе контекста, с указанием источников):";

                // 6. Отправка в LLM
                var answer = await _ollama.GenerateAsync(prompt, cancellationToken);

                // 7. Формирование финального ответа с источниками
                var sources = relevantChunks
                    .Select(c => System.IO.Path.GetFileName(c.Source))
                    .Distinct()
                    .ToList();

                var result = new StringBuilder();
                result.AppendLine(answer);
                result.AppendLine();
                result.AppendLine("---");
                result.AppendLine("📚 **Источники:**");
                foreach (var source in sources)
                {
                    result.AppendLine($"- {source}");
                }
                result.AppendLine();
                result.AppendLine($"*Найдено {relevantChunks.Count} релевантных чанков " +
                                 $"из {chunksList.Count} проверенных*");

                return result.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке вопроса");
                return $"❌ Ошибка при обработке вопроса: {ex.Message}";
            }
        }
    }
}