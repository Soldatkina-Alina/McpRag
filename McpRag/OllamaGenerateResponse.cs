using System.Text.Json.Serialization;

namespace McpRag;

/// <summary>
/// Model for Ollama generate response.
/// </summary>
public class OllamaGenerateResponse
{
    [JsonPropertyName("model")]
    public string Model { get; set; }
    
    [JsonPropertyName("response")]
    public string Response { get; set; }
    
    [JsonPropertyName("done")]
    public bool Done { get; set; }
    
    [JsonPropertyName("error")]
    public string Error { get; set; }
}