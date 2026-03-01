using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace McpRag;

/// <summary>
/// Implementation of IVectorStoreService using ChromaDB.
/// </summary>
public class ChromaDbService : IVectorStoreService
{
    private readonly string _baseUrl = "http://localhost:8000";
    private readonly string _collectionName = "documents";
    private readonly HttpClient _httpClient;
    private readonly IOllamaService _ollama;
    private readonly ILogger<ChromaDbService> _logger;

    public ChromaDbService(HttpClient httpClient, IOllamaService ollama, ILogger<ChromaDbService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _ollama = ollama ?? throw new ArgumentNullException(nameof(ollama));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task AddDocumentsAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding {Count} document chunks to ChromaDB collection: {Collection}", 
            chunks.Count(), _collectionName);

        var request = new
        {
            ids = chunks.Select(c => c.Id).ToList(),
            embeddings = chunks.Select(c => c.Embedding).ToList(),
            documents = chunks.Select(c => c.Text).ToList(),
            metadatas = chunks.Select(c => new Dictionary<string, object>
            {
                ["source"] = c.Source,
                ["chunk_index"] = c.ChunkIndex,
                ["indexed_at"] = c.IndexedAt.ToString("o"),
                ["file_name"] = System.IO.Path.GetFileName(c.Source),
                ["extension"] = System.IO.Path.GetExtension(c.Source)
            }).ToList()
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"/api/v1/collections/{_collectionName}/add", content, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to add documents to ChromaDB: {StatusCode} - {Content}", 
                response.StatusCode, errorContent);
            throw new HttpRequestException($"Failed to add documents: {response.StatusCode} - {errorContent}");
        }

        _logger.LogInformation("Successfully added {Count} document chunks to ChromaDB", chunks.Count());
    }

    public async Task<IEnumerable<DocumentChunk>> SearchAsync(string query, int topK, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching for {TopK} relevant documents with query: {Query}", 
            topK, query);

        var queryEmbedding = await _ollama.GenerateEmbeddingsAsync(query, cancellationToken);
        
        var request = new
        {
            query_embeddings = new[] { queryEmbedding },
            n_results = topK
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"/api/v1/collections/{_collectionName}/query", content, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to search ChromaDB: {StatusCode} - {Content}", 
                response.StatusCode, errorContent);
            throw new HttpRequestException($"Failed to search: {response.StatusCode} - {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var searchResponse = JsonSerializer.Deserialize<ChromaSearchResponse>(responseContent);
        
        if (searchResponse == null || searchResponse.Results == null || !searchResponse.Results.Any())
        {
            _logger.LogWarning("No search results found for query: {Query}", query);
            return Enumerable.Empty<DocumentChunk>();
        }

        var results = searchResponse.Results.First();
        
        var documentChunks = new List<DocumentChunk>();
        
        for (int i = 0; i < results.Documents.Count; i++)
        {
            var metadata = results.Metadatas[i];
            documentChunks.Add(new DocumentChunk
            {
                Id = results.Ids[i],
                Text = results.Documents[i],
                Source = metadata?["source"]?.ToString(),
                ChunkIndex = Convert.ToInt32(metadata?["chunk_index"] ?? "0"),
                IndexedAt = DateTime.Parse(metadata?["indexed_at"]?.ToString() ?? DateTime.UtcNow.ToString()),
                Metadata = metadata?.ToDictionary(x => x.Key, x => x.Value) ?? new()
            });
        }

        _logger.LogInformation("Found {Count} relevant document chunks for query: {Query}", 
            documentChunks.Count, query);
        
        return documentChunks;
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Clearing all documents from ChromaDB collection: {Collection}", _collectionName);

        var request = new
        {
            where = new Dictionary<string, object>()
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"/api/v1/collections/{_collectionName}/delete", content, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to clear ChromaDB collection: {StatusCode} - {Content}", 
                response.StatusCode, errorContent);
            throw new HttpRequestException($"Failed to clear collection: {response.StatusCode} - {errorContent}");
        }

        _logger.LogInformation("Successfully cleared all documents from ChromaDB collection");
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting document count from ChromaDB collection: {Collection}", _collectionName);

        var response = await _httpClient.GetAsync($"/api/v1/collections/{_collectionName}/count", cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to get document count: {StatusCode} - {Content}", 
                response.StatusCode, errorContent);
            throw new HttpRequestException($"Failed to get count: {response.StatusCode} - {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var countResponse = JsonSerializer.Deserialize<ChromaCountResponse>(responseContent);
        
        _logger.LogInformation("ChromaDB collection contains {Count} documents", countResponse.Count);
        
        return countResponse.Count;
    }
}

public class ChromaSearchResponse
{
    public List<ChromaSearchResult> Results { get; set; } = new();
}

public class ChromaSearchResult
{
    public List<string> Ids { get; set; } = new();
    public List<string> Documents { get; set; } = new();
    public List<Dictionary<string, object>> Metadatas { get; set; } = new();
    public List<List<float>> Embeddings { get; set; } = new();
}

public class ChromaCountResponse
{
    public int Count { get; set; }
}