using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text.Json;
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
        return models.Any(m => m.Contains(modelName, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Model for Ollama /api/tags response.
/// </summary>
public class OllamaTagsResponse
{
    public List<OllamaModel> Models { get; set; } = new();
}

public class OllamaModel
{
    public string Name { get; set; } = string.Empty;
    public string Digest { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string ModifiedAt { get; set; } = string.Empty;
}