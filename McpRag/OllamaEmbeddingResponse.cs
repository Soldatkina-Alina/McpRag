using System.Text.Json.Serialization;

namespace McpRag;

/// <summary>
/// Model for Ollama /api/embeddings response.
/// </summary>
public class OllamaEmbeddingResponse
{
    [JsonPropertyName("embedding")]
    public float[] Embedding { get; set; }
}