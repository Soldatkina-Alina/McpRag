using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace McpRag;

    /// <summary>
    /// Представляет фрагмент документа с векторным представлением.
    /// </summary>
    public class DocumentChunk
    {
        /// <summary>
        /// Уникальный идентификатор фрагмента.
        /// </summary>
        public string Id { get; set; } = System.Guid.NewGuid().ToString();

        /// <summary>
        /// Текст содержимого фрагмента.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Путь к исходному файлу.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Индекс фрагмента в исходном документе.
        /// </summary>
        public int ChunkIndex { get; set; }

        /// <summary>
        /// Векторное представление (эмбеддинг) фрагмента.
        /// </summary>
        public float[] Embedding { get; set; }

        /// <summary>
        /// Время индексации фрагмента.
        /// </summary>
        public System.DateTime IndexedAt { get; set; } = System.DateTime.UtcNow;

        /// <summary>
        /// Дополнительные метаданные.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Рейтинг релевантности фрагмента к запросу (от ChromaDB).
        /// </summary>
        public float Score { get; set; }

        /// <summary>
        /// Рейтинг релевантности от LLM (null = не оценено).
        /// </summary>
        public float? LLMScore { get; set; }

        /// <summary>
        /// Финальное решение о релевантности.
        /// </summary>
        public bool IsRelevant { get; set; } = true;

        /// <summary>
        /// Ошибка при оценке фрагмента (если была).
        /// </summary>
        public string GradeError { get; set; }
    }

/// <summary>
/// Represents a search result with score.
/// </summary>
public class SearchResult
{
    /// <summary>
    /// The document chunk.
    /// </summary>
    public DocumentChunk Chunk { get; set; }

    /// <summary>
    /// Relevance score.
    /// </summary>
    public float Score { get; set; }
}

    /// <summary>
    /// Информация о коллекции в векторном хранилище.
    /// </summary>
    public class CollectionInfo
    {
        /// <summary>
        /// Название коллекции.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Количество документов в коллекции.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Время создания коллекции.
        /// </summary>
        public System.DateTime Created { get; set; }
    }

    /// <summary>
    /// Статистика индекса.
    /// </summary>
    public class IndexStatistics
    {
        /// <summary>
        /// Общее количество чанков.
        /// </summary>
        public int TotalChunks { get; set; }

        /// <summary>
        /// Общее количество файлов.
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// Время последней индексации.
        /// </summary>
        public System.DateTime? LastIndexed { get; set; }

        /// <summary>
        /// Список коллекций в хранилище.
        /// </summary>
        public List<CollectionInfo> Collections { get; set; } = new();
    }

/// <summary>
/// Interface for vector store operations.
/// </summary>
public interface IVectorStoreService
{
    /// <summary>
    /// Adds document chunks to the vector store.
    /// </summary>
    /// <param name="chunks">The document chunks to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddDocumentsAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for relevant document chunks based on a query.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="topK">Maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Relevant document chunks.</returns>
    Task<IEnumerable<DocumentChunk>> SearchAsync(string query, int topK, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for relevant document chunks with scores.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="topK">Maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Relevant document chunks with scores.</returns>
    Task<List<SearchResult>> SearchWithScoreAsync(string query, int topK, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all documents from the vector store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ClearAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of documents in the vector store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of documents.</returns>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics about the index.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Index statistics.</returns>
    Task<IndexStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

    /// <summary>
    /// Extension methods for IVectorStoreService.
    /// </summary>
    public static class VectorStoreServiceExtensions
    {
        /// <summary>
        /// Searches for relevant document chunks with scores.
        /// </summary>
        /// <param name="vectorStore">The vector store service.</param>
        /// <param name="query">The search query.</param>
        /// <param name="topK">Maximum number of results to return.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Relevant document chunks with scores.</returns>
        public static async Task<List<SearchResult>> SearchWithScoreAsyncExtension(this IVectorStoreService vectorStore, string query, int topK, CancellationToken cancellationToken = default)
        {
            var results = await vectorStore.SearchAsync(query, topK, cancellationToken);
            return results.Select(c => new SearchResult { Chunk = c, Score = c.Score }).ToList();
        }
    }
