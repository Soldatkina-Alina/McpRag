using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpRag;

public class OllamaTagsResponse
{
    [JsonPropertyName("models")]
    public List<OllamaModel> Models { get; set; } = new();
}

public class OllamaModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("digest")]
    public string Digest { get; set; } = string.Empty;
    
    [JsonPropertyName("size")]
    public long Size { get; set; }
    
    [JsonPropertyName("modified_at")]
    public string ModifiedAt { get; set; } = string.Empty;
}