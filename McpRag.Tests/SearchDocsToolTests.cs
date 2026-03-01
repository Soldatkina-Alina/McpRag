using McpRag.Tools;
using McpRag;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace McpRag.Tests;

public class SearchDocsToolTests
{
    private readonly Mock<IVectorStoreService> _vectorStoreMock;
    private readonly SearchDocsTool _searchDocsTool;

    public SearchDocsToolTests()
    {
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<SearchDocsTool>();
        _vectorStoreMock = new Mock<IVectorStoreService>();
        _searchDocsTool = new SearchDocsTool(_vectorStoreMock.Object, logger);
    }

    [Fact]
    public async Task SearchDocs_WithValidQuery_ShouldReturnResults()
    {
        // Arrange
        var query = "C# programming";
        var topK = 2;
        var expectedChunks = new List<DocumentChunk>
        {
            new DocumentChunk
            {
                Id = "1",
                Text = "C# is a modern, object-oriented programming language",
                Source = "test_docs/csharp_basics.txt",
                ChunkIndex = 0,
                Embedding = new float[10]
            },
            new DocumentChunk
            {
                Id = "2",
                Text = "The .NET Framework is a software framework",
                Source = "test_docs/dotnet_framework.txt",
                ChunkIndex = 0,
                Embedding = new float[10]
            }
        };

        _vectorStoreMock.Setup(x => x.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _vectorStoreMock.Setup(x => x.SearchAsync(query, topK, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedChunks);

        // Act
        var result = await _searchDocsTool.SearchDocs(query, topK);

        // Assert
        Assert.Contains("Найдено 2 релевантных документов", result);
        Assert.Contains("csharp_basics.txt", result);
        Assert.Contains("dotnet_framework.txt", result);
    }

    [Fact]
    public async Task SearchDocs_WithNoResults_ShouldReturnNotFoundMessage()
    {
        // Arrange
        var query = "nonexistent topic";
        var topK = 5;
        _vectorStoreMock.Setup(x => x.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _vectorStoreMock.Setup(x => x.SearchAsync(query, topK, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentChunk>());

        // Act
        var result = await _searchDocsTool.SearchDocs(query, topK);

        // Assert
        Assert.Contains("Не найдено релевантных документов по запросу", result);
    }

    [Fact]
    public async Task SearchDocs_WithEmptyQuery_ShouldNotThrow()
    {
        // Arrange
        var query = string.Empty;
        var topK = 5;
        _vectorStoreMock.Setup(x => x.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _vectorStoreMock.Setup(x => x.SearchAsync(query, topK, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentChunk>());

        // Act & Assert
        var result = await _searchDocsTool.SearchDocs(query, topK);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task SearchDocs_WithZeroTopK_ShouldUseDefault()
    {
        // Arrange
        var query = "C#";
        var topK = 0;
        var expectedChunks = new List<DocumentChunk>
        {
            new DocumentChunk
            {
                Id = "1",
                Text = "C# programming language",
                Source = "test_docs/csharp.txt",
                ChunkIndex = 0,
                Embedding = new float[10]
            }
        };

        _vectorStoreMock.Setup(x => x.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _vectorStoreMock.Setup(x => x.SearchAsync(query, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedChunks);

        // Act
        var result = await _searchDocsTool.SearchDocs(query, topK);

        // Assert
        Assert.Contains("Найдено 1 релевантных документов", result);
    }

    [Fact]
    public async Task SearchDocs_WithNegativeTopK_ShouldUseDefault()
    {
        // Arrange
        var query = "C#";
        var topK = -1;
        var expectedChunks = new List<DocumentChunk>
        {
            new DocumentChunk
            {
                Id = "1",
                Text = "C# programming language",
                Source = "test_docs/csharp.txt",
                ChunkIndex = 0,
                Embedding = new float[10]
            }
        };

        _vectorStoreMock.Setup(x => x.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _vectorStoreMock.Setup(x => x.SearchAsync(query, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedChunks);

        // Act
        var result = await _searchDocsTool.SearchDocs(query, topK);

        // Assert
        Assert.Contains("Найдено 1 релевантных документов", result);
    }
}