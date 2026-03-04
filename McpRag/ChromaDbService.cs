using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
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
    private readonly string _collectionName = "documents";
    private readonly HttpClient _httpClient;
    private readonly IOllamaService _ollama;
    private readonly ILogger<ChromaDbService> _logger;
    private readonly RAGConfig _config;

    // API v2 endpoints
    private readonly string _collectionsEndpoint = "/api/v2/tenants/default_tenant/databases/default_database/collections";

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="ChromaDbService"/>.
    /// </summary>
    /// <param name="httpClient">HTTP-клиент для отправки запросов к ChromaDB API.</param>
    /// <param name="ollama">Сервис для работы с Ollama (генерация эмбеддингов).</param>
    /// <param name="vectorStoreConfig">Конфигурация для VectorStore.</param>
    /// <param name="logger">Логгер для записи информации о работе сервиса.</param>
    /// <exception cref="ArgumentNullException">Выбрасывается, если любой из параметров равен null.</exception>
    public ChromaDbService(HttpClient httpClient, IOllamaService ollama, Microsoft.Extensions.Options.IOptions<VectorStoreConfig> vectorStoreConfig, Microsoft.Extensions.Options.IOptions<RAGConfig> config, ILogger<ChromaDbService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClient.BaseAddress = new Uri(vectorStoreConfig?.Value?.ConnectionString ?? "http://localhost:8000");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _ollama = ollama ?? throw new ArgumentNullException(nameof(ollama));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
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
        var results = await SearchWithScoreAsync(query, topK, cancellationToken);
        return results.Select(r => r.Chunk);
    }

    /// <summary>
    /// Ищет наиболее релевантные фрагменты документов с релевантностью.
    /// </summary>
    /// <param name="query">Текст запроса для поиска.</param>
    /// <param name="topK">Максимальное количество результатов для возврата.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Коллекция найденных фрагментов документов с релевантностью.</returns>
    /// <exception cref="HttpRequestException">Выбрасывается, если запрос к API завершился с ошибкой.</exception>
    public async Task<List<SearchResult>> SearchWithScoreAsync(string query, int topK, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching for {TopK} relevant documents with query: {Query}",
            topK, query);

        // Get collection ID
        var collectionId = await GetCollectionIdAsync(cancellationToken);
        if (string.IsNullOrEmpty(collectionId))
        {
            _logger.LogWarning("Collection {Collection} not found, returning empty results", _collectionName);
            return new List<SearchResult>();
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
        var searchResponse = JsonSerializer.Deserialize<ChromaSearchResult>(responseContent);

        if (searchResponse == null)
        {
            _logger.LogWarning("No search results found for query: {Query}", query);
            return new List<SearchResult>();
        }

        var results = new List<SearchResult>();
        var documentChunks = searchResponse.ToDocumentChunks();

        // Calculate score from distance (lower distance = higher score)
        for (int i = 0; i < documentChunks.Count; i++)
        {
            var chunk = documentChunks[i];
            if (chunk.Metadata.TryGetValue("distance", out var distanceObj))
            {
                var distance = Convert.ToDouble(distanceObj);
                //Релевантность документа: от 0 до 1
            var score = Math.Exp(-distance / _config.Temperature);
                chunk.Score = (float)score;

                results.Add(new SearchResult { Chunk = chunk, Score = (float)score });
            }
            else
            {
                // Если нет distance - присваиваем высокий score по умолчанию
                chunk.Score = 0.9f;
                results.Add(new SearchResult { Chunk = chunk, Score = 0.9f });
            }
            
            _logger.LogDebug("Chunk {Index} - Score: {Score:F2}, Text: {Text}", 
                i, chunk.Score, chunk.Text.Substring(0, Math.Min(50, chunk.Text.Length)));
        }

        _logger.LogInformation("Found {Count} relevant document chunks for query: {Query}",
            results.Count, query);

        return results;
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

        // Get all document IDs first, then delete them
        var allDocsRequest = new { }; // Empty request to get all documents
        var allDocsJson = JsonSerializer.Serialize(allDocsRequest);
        var allDocsContent = new StringContent(allDocsJson, System.Text.Encoding.UTF8, "application/json");
        
        var allDocsResponse = await _httpClient.PostAsync($"{_collectionsEndpoint}/{collectionId}/get", allDocsContent, cancellationToken);
        
        if (!allDocsResponse.IsSuccessStatusCode)
        {
            var errorContent = await allDocsResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to get all documents for clearing: {StatusCode} - {Content}",
                allDocsResponse.StatusCode, errorContent);
            throw new HttpRequestException($"Failed to get documents: {allDocsResponse.StatusCode} - {errorContent}");
        }
        
        var allDocsResult = await allDocsResponse.Content.ReadAsStringAsync(cancellationToken);
        var getResponse = JsonDocument.Parse(allDocsResult);
        var ids = new List<string>();
        
        if (getResponse.RootElement.TryGetProperty("ids", out var idsElement) && idsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var idElement in idsElement.EnumerateArray())
            {
                if (idElement.ValueKind == JsonValueKind.String)
                {
                    ids.Add(idElement.GetString());
                }
            }
        }

        if (ids.Count == 0)
        {
            _logger.LogInformation("Collection {Collection} is already empty", _collectionName);
            return;
        }

        // Delete all documents by IDs
        var deleteRequest = new { ids = ids };
        var deleteJson = JsonSerializer.Serialize(deleteRequest);
        var deleteContent = new StringContent(deleteJson, System.Text.Encoding.UTF8, "application/json");

        var deleteResponse = await _httpClient.PostAsync($"{_collectionsEndpoint}/{collectionId}/delete", deleteContent, cancellationToken);

        if (!deleteResponse.IsSuccessStatusCode)
        {
            var errorContent = await deleteResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to clear ChromaDB collection: {StatusCode} - {Content}",
                deleteResponse.StatusCode, errorContent);
            throw new HttpRequestException($"Failed to clear collection: {deleteResponse.StatusCode} - {errorContent}");
        }

        _logger.LogInformation("Successfully cleared all documents from ChromaDB collection: {Count} documents deleted", ids.Count);
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
    /// Возвращает статистику по индексу.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Статистика по индексу.</returns>
    /// <exception cref="HttpRequestException">Выбрасывается, если запрос к API завершился с ошибкой.</exception>
    public async Task<IndexStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting index statistics");

        var stats = new IndexStatistics();

        // Get collection ID
        var collectionId = await GetCollectionIdAsync(cancellationToken);
        if (string.IsNullOrEmpty(collectionId))
        {
            _logger.LogInformation("Collection {Collection} not found, returning empty statistics", _collectionName);
            return stats;
        }

        // Get all documents to collect statistics
        var allDocsRequest = new { };
        var allDocsJson = JsonSerializer.Serialize(allDocsRequest);
        var allDocsContent = new StringContent(allDocsJson, System.Text.Encoding.UTF8, "application/json");
        
        var allDocsResponse = await _httpClient.PostAsync($"{_collectionsEndpoint}/{collectionId}/get", allDocsContent, cancellationToken);
        
        if (!allDocsResponse.IsSuccessStatusCode)
        {
            var errorContent = await allDocsResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to get all documents for statistics: {StatusCode} - {Content}",
                allDocsResponse.StatusCode, errorContent);
            throw new HttpRequestException($"Failed to get documents: {allDocsResponse.StatusCode} - {errorContent}");
        }
        
        var allDocsResult = await allDocsResponse.Content.ReadAsStringAsync(cancellationToken);
        var getResponse = JsonDocument.Parse(allDocsResult);
        
        // Get collection information
        var collectionsResponse = await _httpClient.GetAsync(_collectionsEndpoint, cancellationToken);
        if (collectionsResponse.IsSuccessStatusCode)
        {
            var collectionsContent = await collectionsResponse.Content.ReadAsStringAsync(cancellationToken);
            var collections = JsonSerializer.Deserialize<List<ChromaCollection>>(collectionsContent);
            
            foreach (var collection in collections ?? new List<ChromaCollection>())
            {
                var collectionStats = new CollectionInfo
                {
                    Name = collection.Name,
                    Count = await GetCollectionDocumentCountAsync(collection.Id, cancellationToken),
                    Created = DateTime.UtcNow // ChromaDB doesn't return creation time, using current time as fallback
                };
                
                stats.Collections.Add(collectionStats);
            }
        }
        
        if (getResponse.RootElement.TryGetProperty("ids", out var idsElement) && idsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var idElement in idsElement.EnumerateArray())
            {
                if (idElement.ValueKind == JsonValueKind.String)
                {
                    stats.TotalChunks++;
                }
                else if (idElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var nestedId in idElement.EnumerateArray())
                    {
                        if (nestedId.ValueKind == JsonValueKind.String)
                        {
                            stats.TotalChunks++;
                        }
                    }
                }
            }
        }
        
        if (getResponse.RootElement.TryGetProperty("metadatas", out var metadatasElement) && metadatasElement.ValueKind == JsonValueKind.Array)
        {
            var uniqueFiles = new HashSet<string>();
            DateTime? lastIndexed = null;
            
            foreach (var metadataElement in metadatasElement.EnumerateArray())
            {
                if (metadataElement.ValueKind == JsonValueKind.Object)
                {
                    // Extract source (file path) to count unique files
                    if (metadataElement.TryGetProperty("source", out var sourceElement) && 
                        sourceElement.ValueKind == JsonValueKind.String)
                    {
                        uniqueFiles.Add(sourceElement.GetString());
                    }
                    
                    // Extract indexed_at to find last indexing time
                    if (metadataElement.TryGetProperty("indexed_at", out var indexedAtElement) && 
                        indexedAtElement.ValueKind == JsonValueKind.String)
                    {
                        if (DateTime.TryParse(indexedAtElement.GetString(), out var indexedAt))
                        {
                            if (!lastIndexed.HasValue || indexedAt > lastIndexed.Value)
                            {
                                lastIndexed = indexedAt;
                            }
                        }
                    }
                }
                else if (metadataElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var nestedMetadata in metadataElement.EnumerateArray())
                    {
                        if (nestedMetadata.ValueKind == JsonValueKind.Object)
                        {
                            // Extract source (file path) to count unique files
                            if (nestedMetadata.TryGetProperty("source", out var sourceElement) && 
                                sourceElement.ValueKind == JsonValueKind.String)
                            {
                                uniqueFiles.Add(sourceElement.GetString());
                            }
                            
                            // Extract indexed_at to find last indexing time
                            if (nestedMetadata.TryGetProperty("indexed_at", out var indexedAtElement) && 
                                indexedAtElement.ValueKind == JsonValueKind.String)
                            {
                                if (DateTime.TryParse(indexedAtElement.GetString(), out var indexedAt))
                                {
                                    if (!lastIndexed.HasValue || indexedAt > lastIndexed.Value)
                                    {
                                        lastIndexed = indexedAt;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            stats.TotalFiles = uniqueFiles.Count;
            stats.LastIndexed = lastIndexed;
        }
        
        _logger.LogInformation("Index statistics: {TotalChunks} chunks, {TotalFiles} files", 
            stats.TotalChunks, stats.TotalFiles);
            
        return stats;
    }
    
    private async Task<int> GetCollectionDocumentCountAsync(string collectionId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"{_collectionsEndpoint}/{collectionId}/count", cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to get count for collection {Id}: {StatusCode}", 
                collectionId, response.StatusCode);
            return 0;
        }
        
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        int.TryParse(responseContent, out int count);
        
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
    [JsonPropertyName("ids")]
    public List<List<string>> Ids { get; set; }

    [JsonPropertyName("embeddings")]
    public object Embeddings { get; set; } // может быть null

    [JsonPropertyName("documents")]
    public List<List<string>> Documents { get; set; }

    [JsonPropertyName("uris")]
    public object Uris { get; set; } // может быть null

    [JsonPropertyName("metadatas")]
    public List<List<ChromaMetadata>> Metadatas { get; set; }

    [JsonPropertyName("distances")]
    public List<List<double>> Distances { get; set; }

    [JsonPropertyName("include")]
    public List<string> Include { get; set; }

    // Преобразование в DocumentChunk
    public List<DocumentChunk> ToDocumentChunks()
    {
        var chunks = new List<DocumentChunk>();

        if (Ids == null || Ids.Count == 0)
            return chunks;

        for (int queryIndex = 0; queryIndex < Ids.Count; queryIndex++)
        {
            var queryIds = Ids[queryIndex];
            var queryDocuments = Documents?[queryIndex] ?? new List<string>();
            var queryMetadatas = Metadatas?[queryIndex] ?? new List<ChromaMetadata>();
            var queryDistances = Distances?[queryIndex] ?? new List<double>();

            for (int i = 0; i < queryIds.Count; i++)
            {
                var metadata = i < queryMetadatas.Count ? queryMetadatas[i] : null;

                var chunk = new DocumentChunk
                {
                    // Используем ID из Chroma или генерируем новый
                    Id = queryIds[i] ?? Guid.NewGuid().ToString(),

                    // Текст документа
                    Text = i < queryDocuments.Count ? queryDocuments[i] : null,

                    // Источник (путь к файлу)
                    Source = metadata?.Source ?? metadata?.FileName,

                    // Индекс чанка
                    ChunkIndex = metadata?.ChunkIndex ?? 0,

                    // Время индексации
                    IndexedAt = metadata?.IndexedAt ?? DateTime.UtcNow,

                    // Embedding пока пустой (Chroma не возвращает embeddings по умолчанию)
                    Embedding = null,

                    // Дополнительные метаданные
                    Metadata = new Dictionary<string, object>
                    {
                        ["distance"] = i < queryDistances.Count ? queryDistances[i] : 0,
                        ["extension"] = metadata?.Extension,
                        ["query_index"] = queryIndex,
                        ["chroma_id"] = queryIds[i]
                    }
                };

                // Добавляем все поля метаданных
                if (metadata != null)
                {
                    chunk.Metadata["file_name"] = metadata.FileName;
                    chunk.Metadata["indexed_at"] = metadata.IndexedAt;
                    chunk.Metadata["chunk_index"] = metadata.ChunkIndex;
                    chunk.Metadata["source"] = metadata.Source;
                    chunk.Metadata["extension"] = metadata.Extension;
                }

                chunks.Add(chunk);
            }
        }

        return chunks;
    }
}

// Класс для метаданных
public class ChromaMetadata
{
    [JsonPropertyName("indexed_at")]
    public DateTime IndexedAt { get; set; }

    [JsonPropertyName("file_name")]
    public string FileName { get; set; }

    [JsonPropertyName("chunk_index")]
    public int ChunkIndex { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; }

    [JsonPropertyName("extension")]
    public string Extension { get; set; }
}

// Класс для отдельного документа
public class ChromaDocument
{
    public string Id { get; set; }
    // Массив документов (в вашем случае там два одинаковых элемента)
    public List<string> Documents { get; set; } = new List<string>();
    public ChromaMetadata Metadata { get; set; }
    public double Distance { get; set; }

    // Для удобства - получить первый документ или объединенный текст
    public string FirstDocument => Documents?.FirstOrDefault();
    public string AllDocuments => Documents != null ? string.Join(" ", Documents) : "";

    public override string ToString()
    {
        return $"ID: {Id}\n" +
               $"Документов: {Documents?.Count ?? 0}\n" +
               $"Первый документ: {FirstDocument?.Substring(0, Math.Min(50, FirstDocument?.Length ?? 0))}...\n" +
               $"Расстояние: {Distance}\n" +
               $"Файл: {Metadata?.FileName}\n" +
               $"Индексирован: {Metadata?.IndexedAt}\n" +
               $"Чанк: {Metadata?.ChunkIndex}\n";
    }
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
