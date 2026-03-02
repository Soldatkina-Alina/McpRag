using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace McpRag;

/// <summary>
/// Реализация интерфейса IVectorStoreService для работы с ChromaDB - векторной базой данных.
/// Предоставляет методы для добавления, поиска, удаления документов и получения статистики.
/// </summary>
public class ChromaDbService : IVectorStoreService
{
    private readonly string _baseUrl = "http://localhost:8000";
    private readonly string _collectionName = "documents";
    private readonly HttpClient _httpClient;
    private readonly IOllamaService _ollama;
    private readonly ILogger<ChromaDbService> _logger;

    // API v2 endpoints
    private readonly string _collectionsEndpoint = "/api/v2/tenants/default_tenant/databases/default_database/collections";

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="ChromaDbService"/>.
    /// </summary>
    /// <param name="httpClient">HTTP-клиент для отправки запросов к ChromaDB API.</param>
    /// <param name="ollama">Сервис для работы с Ollama (генерация эмбеддингов).</param>
    /// <param name="logger">Логгер для записи информации о работе сервиса.</param>
    /// <exception cref="ArgumentNullException">Выбрасывается, если любой из параметров равен null.</exception>
    public ChromaDbService(HttpClient httpClient, IOllamaService ollama, ILogger<ChromaDbService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _ollama = ollama ?? throw new ArgumentNullException(nameof(ollama));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Добавляет набор фрагментов документов в коллекцию ChromaDB.
    /// </summary>
    /// <param name="chunks">Коллекция фрагментов документов для добавления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <exception cref="HttpRequestException">Выбрасывается, если запрос к API завершился с ошибкой.</exception>
    public async Task AddDocumentsAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding {Count} document chunks to ChromaDB collection: {Collection}",
            chunks.Count(), _collectionName);

        // Get collection ID, create if not exists
        var collectionId = await GetOrCreateCollectionIdAsync(cancellationToken);

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

        var response = await _httpClient.PostAsync($"{_collectionsEndpoint}/{collectionId}/add", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to add documents to ChromaDB: {StatusCode} - {Content}",
                response.StatusCode, errorContent);
            throw new HttpRequestException($"Failed to add documents: {response.StatusCode} - {errorContent}");
        }

        _logger.LogInformation("Successfully added {Count} document chunks to ChromaDB", chunks.Count());
    }

    /// <summary>
    /// Ищет наиболее релевантные фрагменты документов для заданного запроса.
    /// </summary>
    /// <param name="query">Текст запроса для поиска.</param>
    /// <param name="topK">Максимальное количество результатов для возврата.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Коллекция найденных фрагментов документов.</returns>
    /// <exception cref="HttpRequestException">Выбрасывается, если запрос к API завершился с ошибкой.</exception>
    public async Task<IEnumerable<DocumentChunk>> SearchAsync(string query, int topK, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching for {TopK} relevant documents with query: {Query}",
            topK, query);

        // Get collection ID
        var collectionId = await GetCollectionIdAsync(cancellationToken);
        if (string.IsNullOrEmpty(collectionId))
        {
            _logger.LogWarning("Collection {Collection} not found, returning empty results", _collectionName);
            return Enumerable.Empty<DocumentChunk>();
        }

        var queryEmbedding = await _ollama.GenerateEmbeddingsAsync(query, cancellationToken);

        var request = new
        {
            query_embeddings = new[] { queryEmbedding },
            n_results = topK
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_collectionsEndpoint}/{collectionId}/query", content, cancellationToken);

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
                ChunkIndex = metadata?["chunk_index"] != null ? int.Parse(metadata["chunk_index"].ToString()) : 0,
                IndexedAt = DateTime.Parse(metadata?["indexed_at"]?.ToString() ?? DateTime.UtcNow.ToString()),
                Metadata = metadata?.ToDictionary(x => x.Key, x => x.Value) ?? new()
            });
        }

        _logger.LogInformation("Found {Count} relevant document chunks for query: {Query}",
            documentChunks.Count, query);

        return documentChunks;
    }

    /// <summary>
    /// Удаляет все документы из коллекции ChromaDB.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <exception cref="HttpRequestException">Выбрасывается, если запрос к API завершился с ошибкой.</exception>
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Clearing all documents from ChromaDB collection: {Collection}", _collectionName);

        // Get collection ID
        var collectionId = await GetCollectionIdAsync(cancellationToken);
        if (string.IsNullOrEmpty(collectionId))
        {
            _logger.LogWarning("Collection {Collection} not found, skipping clear operation", _collectionName);
            return;
        }

        var request = new
        {
            where = new Dictionary<string, object>()
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_collectionsEndpoint}/{collectionId}/delete", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to clear ChromaDB collection: {StatusCode} - {Content}",
                response.StatusCode, errorContent);
            throw new HttpRequestException($"Failed to clear collection: {response.StatusCode} - {errorContent}");
        }

        _logger.LogInformation("Successfully cleared all documents from ChromaDB collection");
    }

    /// <summary>
    /// Возвращает количество документов в коллекции ChromaDB.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Количество документов в коллекции.</returns>
    /// <exception cref="HttpRequestException">Выбрасывается, если запрос к API завершился с ошибкой.</exception>
    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting document count from ChromaDB collection: {Collection}", _collectionName);

        // Get collection ID
        var collectionId = await GetCollectionIdAsync(cancellationToken);
        if (string.IsNullOrEmpty(collectionId))
        {
            _logger.LogInformation("Collection {Collection} not found, returning 0", _collectionName);
            return 0;
        }

        var response = await _httpClient.GetAsync($"{_collectionsEndpoint}/{collectionId}/count", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to get document count: {StatusCode} - {Content}",
                response.StatusCode, errorContent);
            throw new HttpRequestException($"Failed to get count: {response.StatusCode} - {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        int.TryParse(responseContent, out int count);

        _logger.LogInformation("ChromaDB collection contains {Count} documents", count);

        return count;
    }

    /// <summary>
    /// Получает идентификатор коллекции по имени.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Идентификатор коллекции или null, если коллекция не найдена.</returns>
    private async Task<string> GetCollectionIdAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting collection ID for {Collection}", _collectionName);

        var response = await _httpClient.GetAsync(_collectionsEndpoint, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get collections: {StatusCode} - {Content}",
                response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
            return null;
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var collections = JsonSerializer.Deserialize<List<ChromaCollection>>(responseContent);

        var collection = collections?.FirstOrDefault(c => c.Name == _collectionName);
        return collection?.Id;
    }

    /// <summary>
    /// Получает идентификатор коллекции по имени или создает новую коллекцию, если она не существует.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Идентификатор коллекции.</returns>
    /// <exception cref="HttpRequestException">Выбрасывается, если запрос к API завершился с ошибкой.</exception>
    private async Task<string> GetOrCreateCollectionIdAsync(CancellationToken cancellationToken)
    {
        var collectionId = await GetCollectionIdAsync(cancellationToken);

        if (!string.IsNullOrEmpty(collectionId))
        {
            return collectionId;
        }

        _logger.LogInformation("Creating ChromaDB collection: {Collection}", _collectionName);

        var request = new
        {
            name = _collectionName,
            metadata = new Dictionary<string, object>
            {
                ["description"] = "Documents for RAG system"
            }
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_collectionsEndpoint, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // If collection already exists, return existing ID
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _logger.LogWarning("Collection {Collection} already exists, getting existing ID", _collectionName);
                return await GetCollectionIdAsync(cancellationToken);
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to create collection {Collection}: {StatusCode} - {Content}",
                _collectionName, response.StatusCode, errorContent);
            throw new HttpRequestException($"Failed to create collection: {response.StatusCode} - {errorContent}");
        }

        _logger.LogInformation("Collection {Collection} created successfully", _collectionName);

        // Get ID of newly created collection
        return await GetCollectionIdAsync(cancellationToken);
    }
}

/// <summary>
/// Представляет коллекцию в ChromaDB.
/// </summary>
public class ChromaCollection
{
    /// <summary>
    /// Уникальный идентификатор коллекции.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// Название коллекции.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// Метаданные коллекции.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; }

    /// <summary>
    /// Тenant, к которому принадлежит коллекция.
    /// </summary>
    [JsonPropertyName("tenant")]
    public string Tenant { get; set; }

    /// <summary>
    /// База данных, в которой находится коллекция.
    /// </summary>
    [JsonPropertyName("database")]
    public string Database { get; set; }

    /// <summary>
    /// Размерность векторных эмбеддингов в коллекции.
    /// </summary>
    [JsonPropertyName("dimension")]
    public int? Dimension { get; set; }

    /// <summary>
    /// Версия коллекции.
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; }
}

/// <summary>
/// Ответ от ChromaDB на запрос поиска.
/// </summary>
public class ChromaSearchResponse
{
    /// <summary>
    /// Список результатов поиска.
    /// </summary>
    public List<ChromaSearchResult> Results { get; set; } = new();
}

/// <summary>
/// Результаты поиска в ChromaDB.
/// </summary>
public class ChromaSearchResult
{
    /// <summary>
    /// Идентификаторы найденных документов.
    /// </summary>
    public List<string> Ids { get; set; } = new();

    /// <summary>
    /// Тексты найденных документов.
    /// </summary>
    public List<string> Documents { get; set; } = new();

    /// <summary>
    /// Метаданные найденных документов.
    /// </summary>
    public List<Dictionary<string, object>> Metadatas { get; set; } = new();

    /// <summary>
    /// Эмбеддинги найденных документов.
    /// </summary>
    public List<List<float>> Embeddings { get; set; } = new();
}

/// <summary>
/// Ответ от ChromaDB на запрос получения количества документов.
/// </summary>
public class ChromaCountResponse
{
    /// <summary>
    /// Количество документов в коллекции.
    /// </summary>
    public int Count { get; set; }
}
