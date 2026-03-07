namespace McpRag;

/// <summary>
/// Configuration for vector store.
/// </summary>
public class VectorStoreConfig
{
    public string Type { get; set; } = "chromadb";
    public string ConnectionString { get; set; } = "http://localhost:8000";
    public string CollectionName { get; set; } = "documents";

}