using McpRag;
using McpRag.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;

var builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Add configuration
builder.Services.Configure<OllamaConfig>(builder.Configuration.GetSection("Ollama"));
builder.Services.Configure<IndexerConfig>(builder.Configuration.GetSection("Indexer"));
builder.Services.Configure<VectorStoreConfig>(builder.Configuration.GetSection("VectorStore"));

// Add HttpClient with configuration
builder.Services.AddHttpClient<IOllamaService, OllamaService>((sp, client) =>
{
    var config = sp.GetRequiredService<IOptions<OllamaConfig>>().Value;
    client.BaseAddress = new Uri(config.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
});

// Add vector store
builder.Services.AddSingleton<IVectorStoreService, ChromaDbService>();

// Add indexer service
builder.Services.AddSingleton<IIndexerService, IndexerService>();

// Add the MCP services: the transport to use (stdio) and the tools to register.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<EchoTools>()
    .WithTools<IndexFolderTools>()
    .WithTools<CheckOllamaTool>()
    .WithTools<AskLlmTool>()
    .WithTools<ListFilesTool>()
    .WithTools<SearchDocsTool>()
    .WithTools<VectorStoreStatusTool>();

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Checking services availability...");

// Check Ollama availability
try
{
    var ollamaService = host.Services.GetRequiredService<IOllamaService>();
    await ollamaService.GenerateEmbeddingsAsync("test", default);
    logger.LogInformation("Ollama service is available");
}
catch (Exception ex)
{
    logger.LogError("Ollama service is unavailable: {Error}", ex.Message);
    return;
}

// Check ChromaDB availability
try
{
    var vectorStore = host.Services.GetRequiredService<IVectorStoreService>();
    await vectorStore.CountAsync(default);
    logger.LogInformation("ChromaDB service is available");
}
catch (Exception ex)
{
    logger.LogError("ChromaDB service is unavailable: {Error}", ex.Message);
    return;
}

logger.LogInformation("All services are available. Server started");

await host.RunAsync();