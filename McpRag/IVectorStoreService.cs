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
}