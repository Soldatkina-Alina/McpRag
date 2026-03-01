using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpRag;

public class OllamaService : IOllamaService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaConfig _config;
    private readonly ILogger<OllamaService> _logger;

    public OllamaService(HttpClient httpClient, IOptions<OllamaConfig> config, ILogger<OllamaService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama health check failed");
            return false;
        }
    }

    public async Task<List<string>> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to list models, status code: {StatusCode}", response.StatusCode);
                return new List<string>();
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var tagsResponse = JsonSerializer.Deserialize<OllamaTagsResponse>(responseContent);
            
            return tagsResponse?.Models?.Select(m => m.Name).ToList() ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list Ollama models");
            return new List<string>();
        }
    }

    public async Task<bool> IsModelAvailableAsync(string modelName, CancellationToken cancellationToken = default)
    {
        var models = await ListModelsAsync(cancellationToken);
        return models.Any(m => m.StartsWith(modelName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new
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

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/generate", content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ollama generate failed, status code: {StatusCode}", response.StatusCode);
                return "Извините, не удалось получить ответ от модели.";
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var generateResponse = JsonSerializer.Deserialize<OllamaGenerateResponse>(responseContent);
            
            if (generateResponse == null || string.IsNullOrEmpty(generateResponse.Response))
            {
                _logger.LogWarning("Ollama returned empty response");
                return "Извините, модель вернула пустой ответ.";
            }

            return generateResponse.Response.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama generation failed");
            return "Извините, произошла ошибка при генерации ответа.";
        }
    }

    public async Task<float[]> GenerateEmbeddingsAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new
            {
                model = _config.EmbeddingModel,
                prompt = text
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/embeddings", content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ollama embeddings failed, status code: {StatusCode}", response.StatusCode);
                throw new HttpRequestException($"Failed to generate embeddings, status code: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var embeddingResponse = JsonSerializer.Deserialize<OllamaEmbeddingResponse>(responseContent);
            
            if (embeddingResponse == null || embeddingResponse.Embedding == null)
            {
                _logger.LogWarning("Ollama returned null embedding");
                throw new InvalidOperationException("Ollama returned null embedding");
            }

            return embeddingResponse.Embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama embedding generation failed");
            throw;
        }
    }
}