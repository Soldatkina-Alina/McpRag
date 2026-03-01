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
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            });

        // Act
        await _chromaDbService.AddDocumentsAsync(chunks, CancellationToken.None);

        // Assert
        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.AbsolutePath.Contains("/add")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnResults()
    {
        // Arrange
        var query = "Test query";
        var queryEmbedding = new float[10];
        _ollamaMock.Setup(x => x.GenerateEmbeddingsAsync(query, CancellationToken.None))
            .ReturnsAsync(queryEmbedding);

        var searchResponse = new ChromaSearchResponse
        {
            Results = new List<ChromaSearchResult>
            {
                new ChromaSearchResult
                {
                    Ids = new List<string> { "1" },
                    Documents = new List<string> { "Test document" },
                    Metadatas = new List<Dictionary<string, object>>
                    {
                        new Dictionary<string, object>
                        {
                            { "source", "test1.txt" },
                            { "chunk_index", "0" },
                            { "indexed_at", System.DateTime.UtcNow.ToString("o") }
                        }
                    },
                    Embeddings = new List<List<float>> { new List<float> { 0.1f, 0.2f, 0.3f } }
                }
            }
        };

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(searchResponse))
            });

        // Act
        var results = await _chromaDbService.SearchAsync(query, 5, CancellationToken.None);

        // Assert
        Assert.NotNull(results);
        Assert.NotEmpty(results);
        Assert.Equal("Test document", results.First().Text);
    }

    [Fact]
    public async Task ClearAsync_ShouldClearAllDocuments()
    {
        // Arrange
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            });

        // Act
        await _chromaDbService.ClearAsync(CancellationToken.None);

        // Assert
        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.AbsolutePath.Contains("/delete")),
            ItExpr.IsAny<CancellationToken>());
    }

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
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(countResponse))
            });

        // Act
        var count = await _chromaDbService.CountAsync(CancellationToken.None);

        // Assert
        Assert.Equal(0, count);
    }
}