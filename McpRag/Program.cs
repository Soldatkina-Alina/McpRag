using McpRag;
using McpRag.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System.Net.Http;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("McpRag", LogEventLevel.Information)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        formatter: new CompactJsonFormatter(),
        path: "logs/app-.log",
        rollingInterval: RollingInterval.Day,
        fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB
        retainedFileCountLimit: 30,
        rollOnFileSizeLimit: true
    )
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger);

// Add configuration
builder.Services.Configure<OllamaConfig>(builder.Configuration.GetSection("Ollama"));
builder.Services.Configure<IndexerConfig>(builder.Configuration.GetSection("Indexer"));
builder.Services.Configure<VectorStoreConfig>(builder.Configuration.GetSection("VectorStore"));
builder.Services.Configure<RAGConfig>(builder.Configuration.GetSection("RAG"));

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

// Add RAG services
builder.Services.AddSingleton<ContextFormatter>();
builder.Services.AddScoped<IRagGraphService, RagGraphService>();

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
    .WithTools<VectorStoreStatusTool>()
    .WithTools<AskQuestionTool>()
    .WithTools<FindRelevantDocsTool>()
    .WithTools<SummarizeDocumentTool>()
    .WithTools<IndexStatusTool>();

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