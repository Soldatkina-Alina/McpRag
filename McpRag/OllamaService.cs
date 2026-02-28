using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace McpRag;

/// <summary>
/// Implementation of IOllamaService using HttpClient.
/// </summary>
public class OllamaService : IOllamaService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaConfig _config;
    private readonly ILogger<OllamaService> _logger;

    public OllamaService(HttpClient httpClient, IOptions<OllamaConfig> config, ILogger<OllamaService> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(config.Value.BaseUrl);
        _config = config.Value;
        _logger = logger;
    }

    public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Checking Ollama health at {BaseUrl}", _config.BaseUrl);
            var response = await _httpClient.GetAsync("/api/tags", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama health check failed: {Message}", ex.Message);
            return false;
        }
    }

    public async Task<List<string>> ListModelsAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting list of available models from Ollama");
            var response = await _httpClient.GetAsync("/api/tags", ct);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get models: {StatusCode}", response.StatusCode);
                return new List<string>();
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            var tagsResponse = JsonSerializer.Deserialize<OllamaTagsResponse>(content);
            
            if (tagsResponse?.Models == null)
            {
                _logger.LogWarning("No models found in Ollama response");
                return new List<string>();
            }

            var modelNames = tagsResponse.Models.Select(m => m.Name).ToList();
            _logger.LogInformation("Found {Count} models in Ollama", modelNames.Count);
            return modelNames;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Ollama models: {Message}", ex.Message);
            return new List<string>();
        }
    }

    public async Task<bool> IsModelAvailableAsync(string modelName, CancellationToken ct = default)
    {
        _logger.LogInformation("Checking if model {ModelName} is available in Ollama", modelName);
        
        var models = await ListModelsAsync(ct);
        return models.Any(m => m.StartsWith(modelName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<string> GenerateAsync(string prompt, CancellationToken ct = default)
    {
        _logger.LogInformation("Generating response from Ollama using model {Model}", _config.Model);
        
        var requestBody = new
        {
            model = _config.Model,
            prompt = prompt,
            stream = false,
            options = new
            {
                temperature = _config.Temperature,
                num_predict = _config.MaxTokens
            }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            System.Text.Encoding.UTF8,
            "application/json");

        try
        {
            var response = await _httpClient.PostAsync("/api/generate", content, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Ollama generate failed: {StatusCode}", response.StatusCode);
                throw new HttpRequestException($"Ollama API returned status code {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(ct);
            var generateResponse = JsonSerializer.Deserialize<OllamaGenerateResponse>(responseContent);
            
            if (!string.IsNullOrEmpty(generateResponse?.Error))
            {
                _logger.LogError("Ollama generate error: {Error}", generateResponse.Error);
                throw new Exception(generateResponse.Error);
            }

            if (string.IsNullOrEmpty(generateResponse?.Response))
            {
                _logger.LogWarning("Ollama returned empty response");
                return "Sorry, I didn't get a response from the model.";
            }

            _logger.LogInformation("Ollama generate succeeded, response length: {Length}", generateResponse.Response.Length);
            return generateResponse.Response.Trim();
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Ollama generate operation was canceled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Ollama generate: {Message}", ex.Message);
            throw;
        }
    }
}

/// <summary>
/// Model for Ollama /api/tags response.
/// </summary>
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
    public long Size { get; set; } = 0;
    [JsonPropertyName("modified_at")]
    public string ModifiedAt { get; set; } = string.Empty;
}
