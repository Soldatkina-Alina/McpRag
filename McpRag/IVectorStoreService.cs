using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace McpRag;

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
}

/// <summary>
/// Represents a document chunk with embedding.
/// </summary>
public class DocumentChunk
{
    /// <summary>
    /// Unique identifier for the chunk.
    /// </summary>
    public string Id { get; set; } = System.Guid.NewGuid().ToString();

    /// <summary>
    /// Text content of the chunk.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Source file path.
    /// </summary>
    public string Source { get; set; }

    /// <summary>
    /// Chunk index in the source document.
    /// </summary>
    public int ChunkIndex { get; set; }

    /// <summary>
    /// Embedding vector.
    /// </summary>
    public float[] Embedding { get; set; }

    /// <summary>
    /// Indexed timestamp.
    /// </summary>
    public System.DateTime IndexedAt { get; set; } = System.DateTime.UtcNow;

    /// <summary>
    /// Additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Relevance score of the chunk to the query.
    /// </summary>
    public float Score { get; set; }
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
    public static async Task<List<SearchResult>> SearchWithScoreAsync(this IVectorStoreService vectorStore, string query, int topK, CancellationToken cancellationToken = default)
    {
        var results = await vectorStore.SearchAsync(query, topK, cancellationToken);
        return results.Select(c => new SearchResult { Chunk = c, Score = 0.0f }).ToList();
    }
}
