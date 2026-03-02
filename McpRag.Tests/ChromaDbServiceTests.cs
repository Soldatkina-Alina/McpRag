using McpRag;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace McpRag.Tests;

/// <summary>
/// Тесты для ChromaDbService - сервиса для работы с векторным хранилищем ChromaDB.
/// Проверяют основные операции с хранилищем: добавление, поиск, очистка и подсчет документов.
/// </summary>
public class ChromaDbServiceTests
{
    private readonly Mock<IOllamaService> _ollamaMock;
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly ChromaDbService _chromaDbService;

    public ChromaDbServiceTests()
    {
        _ollamaMock = new Mock<IOllamaService>();
        _httpHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpHandlerMock.Object)
        {
            BaseAddress = new System.Uri("http://localhost:8000")
        };
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<ChromaDbService>();
        _chromaDbService = new ChromaDbService(_httpClient, _ollamaMock.Object, logger);
    }

    /// <summary>
    /// Проверяет, что метод AddDocumentsAsync корректно отправляет запрос на добавление документов в ChromaDB.
    /// Убеждается, что запрос содержит правильные данные и отправляется один раз.
    /// </summary>
    [Fact]
    public async Task AddDocumentsAsync_ShouldAddDocuments()
    {
        // Arrange
        var chunks = new List<DocumentChunk>
        {
            new DocumentChunk
            {
                Id = "1",
                Text = "Test document 1",
                Source = "test1.txt",
                ChunkIndex = 0,
                Embedding = new float[10]
            },
            new DocumentChunk
            {
                Id = "2",
                Text = "Test document 2",
                Source = "test2.txt",
                ChunkIndex = 0,
                Embedding = new float[10]
            }
        };

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                if (req.RequestUri.AbsolutePath.EndsWith("/collections"))
                {
                    var collections = new List<ChromaCollection>
                    {
                        new ChromaCollection
                        {
                            Id = "1",
                            Name = "documents",
                            Metadata = new Dictionary<string, object>(),
                            Tenant = "default_tenant",
                            Database = "default_database"
                        }
                    };
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonSerializer.Serialize(collections))
                    };
                }
                else if (req.RequestUri.AbsolutePath.Contains("/add"))
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("{}")
                    };
                }
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Content = new StringContent("{}")
                };
            });

        // Act
        await _chromaDbService.AddDocumentsAsync(chunks, CancellationToken.None);

        // Assert
        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(), // Once to add documents (get collections is separate verify)
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.AbsolutePath.Contains("/add")),
            ItExpr.IsAny<CancellationToken>());
    }

    /// <summary>
    /// Проверяет, что метод SearchAsync корректно ищет документы в ChromaDB по запросу.
    /// Убеждается, что поиск возвращает результаты и они содержат ожидаемый текст.
    /// </summary>
    [Fact]
    public async Task SearchAsync_ShouldReturnResults()
    {
        // Arrange
        var query = "Test query";
        var queryEmbedding = new float[10];
        _ollamaMock.Setup(x => x.GenerateEmbeddingsAsync(query, CancellationToken.None))
            .ReturnsAsync(queryEmbedding);

        var searchResponse = new ChromaSearchResult
        {
            Ids = new List<List<string>> { new List<string> { "1" } },
            Documents = new List<List<string>> { new List<string> { "Test document" } },
            Metadatas = new List<List<ChromaMetadata>>
            {
                new List<ChromaMetadata>
                {
                    new ChromaMetadata
                    {
                        Source = "test1.txt",
                        ChunkIndex = 0,
                        IndexedAt = System.DateTime.UtcNow
                    }
                }
            },
            Embeddings = new List<List<float>> { new List<float> { 0.1f, 0.2f, 0.3f } }
        };

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                if (req.RequestUri.AbsolutePath.EndsWith("/collections"))
                {
                    var collections = new List<ChromaCollection>
                    {
                        new ChromaCollection
                        {
                            Id = "1",
                            Name = "documents",
                            Metadata = new Dictionary<string, object>(),
                            Tenant = "default_tenant",
                            Database = "default_database"
                        }
                    };
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonSerializer.Serialize(collections))
                    };
                }
                else if (req.RequestUri.AbsolutePath.Contains("/query"))
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonSerializer.Serialize(searchResponse))
                    };
                }
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Content = new StringContent("{}")
                };
            });

        // Act
        var results = await _chromaDbService.SearchAsync(query, 5, CancellationToken.None);

        // Assert
        Assert.NotNull(results);
        Assert.NotEmpty(results);
        Assert.Equal("Test document", results.First().Text);
    }

    /// <summary>
    /// Проверяет, что метод ClearAsync корректно отправляет запрос на очистку всех документов в ChromaDB.
    /// Убеждается, что запрос отправляется дважды: один раз для получения всех документов, второй для удаления.
    /// </summary>
    [Fact]
    public async Task ClearAsync_ShouldClearAllDocuments()
    {
        // Arrange
        var getResponse = new { ids = new[] { "1", "2", "3" }, documents = new[] { "doc1", "doc2", "doc3" } };
        
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                if (req.RequestUri.AbsolutePath.EndsWith("/collections"))
                {
                    var collections = new List<ChromaCollection>
                    {
                        new ChromaCollection
                        {
                            Id = "1",
                            Name = "documents",
                            Metadata = new Dictionary<string, object>(),
                            Tenant = "default_tenant",
                            Database = "default_database"
                        }
                    };
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonSerializer.Serialize(collections))
                    };
                }
                else if (req.RequestUri.AbsolutePath.Contains("/get"))
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonSerializer.Serialize(getResponse))
                    };
                }
                else if (req.RequestUri.AbsolutePath.Contains("/delete"))
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("{}")
                    };
                }
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Content = new StringContent("{}")
                };
            });

        // Act
        await _chromaDbService.ClearAsync(CancellationToken.None);

        // Assert
        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(3), // Once to get collections, once to get all documents, once to delete them
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    /// <summary>
    /// Проверяет, что метод CountAsync возвращает 0 при пустом хранилище.
    /// Убеждается, что сервис корректно обрабатывает ответ от ChromaDB и возвращает ожидаемое значение.
    /// </summary>
    [Fact]
    public async Task CountAsync_ShouldReturnZero_WhenEmpty()
    {
        // Arrange
        var countResponse = new { count = 0 };

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                if (req.RequestUri.AbsolutePath.EndsWith("/collections"))
                {
                    var collections = new List<ChromaCollection>
                    {
                        new ChromaCollection
                        {
                            Id = "1",
                            Name = "documents",
                            Metadata = new Dictionary<string, object>(),
                            Tenant = "default_tenant",
                            Database = "default_database"
                        }
                    };
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonSerializer.Serialize(collections))
                    };
                }
                else if (req.RequestUri.AbsolutePath.Contains("/count"))
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonSerializer.Serialize(countResponse))
                    };
                }
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Content = new StringContent("{}")
                };
            });

        // Act
        var count = await _chromaDbService.CountAsync(CancellationToken.None);

        // Assert
        Assert.Equal(0, count);
    }
}