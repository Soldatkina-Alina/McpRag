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
    .WriteTo.File(
        formatter: new CompactJsonFormatter(),
        path: "logs/app-.log",
        rollingInterval: RollingInterval.Day,
        fileSizeLimitBytes: 100 * 1024 * 1024, // 100MB (увеличим размер файла)
        retainedFileCountLimit: null, // не удалять старые логи
        rollOnFileSizeLimit: true,
        restrictedToMinimumLevel: LogEventLevel.Information
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
builder.Services.AddHttpClient<IVectorStoreService, ChromaDbService>((sp, client) =>
{
    var config = sp.GetRequiredService<IOptions<VectorStoreConfig>>().Value;
    client.BaseAddress = new Uri(config.ConnectionString);
    client.Timeout = TimeSpan.FromSeconds(600); // Увеличим до 10 минут
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

// Skip service availability checking to start server quietly
// Services will be checked on first use

await host.RunAsync();
