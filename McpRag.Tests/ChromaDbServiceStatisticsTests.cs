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
/// Тесты для ChromaDbService.GetStatisticsAsync.
/// Проверяют работу с получением статистики индекса из ChromaDB.
/// </summary>
public class ChromaDbServiceStatisticsTests
{
    private readonly Mock<IOllamaService> _ollamaMock;
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly ChromaDbService _chromaDbService;

    public ChromaDbServiceStatisticsTests()
    {
        _ollamaMock = new Mock<IOllamaService>();
        _httpHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpHandlerMock.Object)
        {
            BaseAddress = new System.Uri("http://localhost:8000")
        };
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<ChromaDbService>();
        var config = Microsoft.Extensions.Options.Options.Create(new RAGConfig());
        _chromaDbService = new ChromaDbService(_httpClient, _ollamaMock.Object, config, logger);
    }

    /// <summary>
    /// Проверяет, что метод GetStatisticsAsync возвращает статистику при наличии документов.
    /// Убеждается, что возвращаемые данные корректно разбираются и содержат ожидаемую информацию.
    /// </summary>
    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnStatistics_WhenDocumentsExist()
    {
        // Arrange
        var collectionsResponse = new List<ChromaCollection>
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

        var getResponse = new
        {
            ids = new[] { "1", "2", "3" },
            documents = new[] { "doc1", "doc2", "doc3" },
            metadatas = new[]
            {
                new { source = "test1.txt", indexed_at = "2023-10-01T12:00:00Z" },
                new { source = "test1.txt", indexed_at = "2023-10-01T12:00:00Z" },
                new { source = "test2.txt", indexed_at = "2023-10-02T10:00:00Z" }
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
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonSerializer.Serialize(collectionsResponse))
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
                else if (req.RequestUri.AbsolutePath.Contains("/count"))
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("3")
                    };
                }
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Content = new StringContent("{}")
                };
            });

        // Act
        var stats = await _chromaDbService.GetStatisticsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(3, stats.TotalChunks);
        Assert.Equal(2, stats.TotalFiles); // test1.txt and test2.txt
        Assert.NotNull(stats.LastIndexed);
        Assert.Equal(2023, stats.LastIndexed.Value.Year);
        Assert.Equal(10, stats.LastIndexed.Value.Month);
        Assert.Equal(2, stats.LastIndexed.Value.Day);
        Assert.NotNull(stats.Collections);
        Assert.Single(stats.Collections);
        Assert.Equal("documents", stats.Collections.First().Name);
        Assert.Equal(3, stats.Collections.First().Count);
    }

    /// <summary>
    /// Проверяет, что метод GetStatisticsAsync возвращает пустую статистику при отсутствии документов.
    /// Убеждается, что корректно обрабатывается случай с пустым индексом.
    /// </summary>
    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnEmptyStatistics_WhenNoDocuments()
    {
        // Arrange
        var collectionsResponse = new List<ChromaCollection>
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

        var getResponse = new
        {
            ids = new string[] { },
            documents = new string[] { },
            metadatas = new object[] { }
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
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonSerializer.Serialize(collectionsResponse))
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
                else if (req.RequestUri.AbsolutePath.Contains("/count"))
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("0")
                    };
                }
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Content = new StringContent("{}")
                };
            });

        // Act
        var stats = await _chromaDbService.GetStatisticsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(0, stats.TotalChunks);
        Assert.Equal(0, stats.TotalFiles);
        Assert.Null(stats.LastIndexed);
        Assert.NotNull(stats.Collections);
        Assert.Single(stats.Collections);
        Assert.Equal("documents", stats.Collections.First().Name);
        Assert.Equal(0, stats.Collections.First().Count);
    }

    /// <summary>
    /// Проверяет, что метод GetStatisticsAsync возвращает пустую статистику при отсутствии коллекции.
    /// Убеждается, что корректно обрабатывается случай с несуществующей коллекцией.
    /// </summary>
    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnEmptyStatistics_WhenCollectionNotFound()
    {
        // Arrange
        var collectionsResponse = new List<ChromaCollection>();

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                if (req.RequestUri.AbsolutePath.EndsWith("/collections"))
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonSerializer.Serialize(collectionsResponse))
                    };
                }
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Content = new StringContent("{}")
                };
            });

        // Act
        var stats = await _chromaDbService.GetStatisticsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(0, stats.TotalChunks);
        Assert.Equal(0, stats.TotalFiles);
        Assert.Null(stats.LastIndexed);
        Assert.NotNull(stats.Collections);
        Assert.Empty(stats.Collections);
    }
}