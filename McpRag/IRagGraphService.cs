using System.Threading;
using System.Threading.Tasks;

namespace McpRag;

/// <summary>
/// Интерфейс для сервиса графа RAG.
/// </summary>
public interface IRagGraphService
{
    /// <summary>
    /// Выполняет график RAG для получения ответа на вопрос.
    /// </summary>
    /// <param name="question">Вопрос пользователя.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Состояние графа RAG.</returns>
    Task<RagState> ExecuteAsync(string question, CancellationToken ct = default);
}