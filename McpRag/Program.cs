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

// Add configuration with environment variables support
builder.Services.Configure<OllamaConfig>(options =>
{
    options.BaseUrl = Environment.GetEnvironmentVariable("OLLAMA_HOST") ?? "http://localhost:11434";
    options.Model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "phi3:mini";
    options.EmbeddingModel = Environment.GetEnvironmentVariable("OLLAMA_EMBEDDING_MODEL") ?? "nomic-embed-text";
    if (int.TryParse(Environment.GetEnvironmentVariable("OLLAMA_TIMEOUT"), out int timeout))
        options.TimeoutSeconds = timeout;
});

builder.Services.Configure<VectorStoreConfig>(options =>
{
    options.ConnectionString = Environment.GetEnvironmentVariable("CHROMADB_HOST") ?? "http://localhost:8000";
});

builder.Services.Configure<IndexerConfig>(builder.Configuration.GetSection("Indexer"));
builder.Services.Configure<RAGConfig>(builder.Configuration.GetSection("RAG"));

// Add HttpClient with configuration
builder.Services.AddHttpClient<IOllamaService, OllamaService>((sp, client) =>
{
    var config = sp.GetRequiredService<IOptions<OllamaConfig>>().Value;
    client.BaseAddress = new Uri(config.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
});

// Add vector store
builder.Services.AddHttpClient<IVectorStoreService, ChromaDbService>();
builder.Services.AddSingleton<IVectorStoreService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    var ollamaService = sp.GetRequiredService<IOllamaService>();
    var vectorStoreConfig = sp.GetRequiredService<IOptions<VectorStoreConfig>>();
    var ragConfig = sp.GetRequiredService<IOptions<RAGConfig>>();
    var logger = sp.GetRequiredService<ILogger<ChromaDbService>>();
    return new ChromaDbService(httpClient, ollamaService, vectorStoreConfig, ragConfig, logger);
});

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

// Check services availability with retries
int maxRetries = 10;
int retryDelay = 3;

// Check Ollama availability
bool ollamaAvailable = false;
for (int i = 0; i < maxRetries; i++)
{
    try
    {
        var ollamaService = host.Services.GetRequiredService<IOllamaService>();
        await ollamaService.GenerateEmbeddingsAsync("test", default);
        logger.LogInformation("Ollama service is available");
        ollamaAvailable = true;
        break;
    }
    catch (Exception ex)
    {
        logger.LogWarning("Ollama service is unavailable (attempt {Attempt}/{MaxRetries}): {Error}", i + 1, maxRetries, ex.Message);
        if (i < maxRetries - 1)
            await Task.Delay(TimeSpan.FromSeconds(retryDelay));
    }
}

if (!ollamaAvailable)
{
    logger.LogWarning("Ollama service is still unavailable after {MaxRetries} attempts. Starting in degraded mode...", maxRetries);
    // Continue without Ollama
}

// Check ChromaDB availability
bool chromaAvailable = false;
for (int i = 0; i < maxRetries; i++)
{
    try
    {
        var vectorStore = host.Services.GetRequiredService<IVectorStoreService>();
        await vectorStore.CountAsync(default);
        logger.LogInformation("ChromaDB service is available");
        chromaAvailable = true;
        break;
    }
    catch (Exception ex)
    {
        logger.LogWarning("ChromaDB service is unavailable (attempt {Attempt}/{MaxRetries}): {Error}", i + 1, maxRetries, ex.Message);
        if (i < maxRetries - 1)
            await Task.Delay(TimeSpan.FromSeconds(retryDelay));
    }
}

if (!chromaAvailable)
{
    logger.LogWarning("ChromaDB service is still unavailable after {MaxRetries} attempts. Starting in degraded mode...", maxRetries);
    // Continue without ChromaDB
}

logger.LogInformation("Server started");

await host.RunAsync();
