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
    public class AskQuestionTool
    {
        private readonly IRagGraphService _ragGraph;
        private readonly RAGConfig _config;
        private readonly ILogger<AskQuestionTool> _logger;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="AskQuestionTool"/>.
        /// </summary>
        /// <param name="ragGraph">Сервис для выполнения графа RAG.</param>
        /// <param name="config">Конфигурация RAG.</param>
        /// <param name="logger">Логгер.</param>
        public AskQuestionTool(
            IRagGraphService ragGraph,
            IOptions<RAGConfig> config,
            ILogger<AskQuestionTool> logger)
        {
            _ragGraph = ragGraph ?? throw new ArgumentNullException(nameof(ragGraph));
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Получает ответ на вопрос из базы знаний.
        /// </summary>
        /// <param name="question">Вопрос к базе знаний.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Ответ на вопрос.</returns>
        [McpServerTool]
        [Description("Получает ответ на вопрос из базы знаний с использованием RAG")]
        public async Task<string> AskQuestion(
            [Description("Вопрос к базе знаний")] string question,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var state = await _ragGraph.ExecuteAsync(question, cancellationToken);
                
                if (state.HasError)
                {
                    return $"❌ Ошибка: {state.ErrorMessage}";
                }
                
                var result = new StringBuilder();
                result.AppendLine(state.Answer);
                result.AppendLine();
                result.AppendLine("---");
                result.AppendLine("📊 **Путь выполнения:**");
                
                foreach (var step in state.ExecutionSteps)
                {
                    result.AppendLine($"- **{step.NodeName}** ({step.Timestamp:HH:mm:ss})");
                    if (step.Metadata.Any())
                    {
                        var meta = string.Join(", ", step.Metadata.Select(m => $"{m.Key}: {m.Value}"));
                        result.AppendLine($"  *{meta}*");
                    }
                }
                
                if (state.ExecutionSteps.Any(s => s.NodeName == "GradeDocuments"))
                {
                    var gradeStep = state.ExecutionSteps.First(s => s.NodeName == "GradeDocuments");
                    result.AppendLine();
                    if (gradeStep.Metadata.ContainsKey("relevant_after_grade") && gradeStep.Metadata.ContainsKey("total_docs"))
                    {
                        result.AppendLine($"📊 **Оценка релевантности:** {gradeStep.Metadata["relevant_after_grade"]}/{gradeStep.Metadata["total_docs"]} документов");
                    }
                    if (gradeStep.Metadata.ContainsKey("grade_errors"))
                    {
                        result.AppendLine($"   *Ошибок оценки: {gradeStep.Metadata["grade_errors"]}*");
                    }
                }
                
                if (state.Documents.Any())
                {
                    var sources = state.Documents
                        .Where(d => d.Score >= _config.MinRelevanceScore)
                        .Select(d => System.IO.Path.GetFileName(d.Source))
                        .Distinct();
                    
                    result.AppendLine();
                    result.AppendLine("📚 **Источники:**");
                    foreach (var source in sources)
                    {
                        result.AppendLine($"- {source}");
                    }
                    
                    result.AppendLine();
                    result.AppendLine($"*Найдено {state.Documents.Count(d => d.Score >= _config.MinRelevanceScore)} " +
                                     $"релевантных чанков из {state.Documents.Count} проверенных*");
                }
                
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
