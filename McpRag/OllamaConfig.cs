namespace McpRag;

/// <summary>
/// Configuration class for Ollama service settings.
/// </summary>
public class OllamaConfig
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "phi3:mini";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
    public int TimeoutSeconds { get; set; } = 30;
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 500;
}
