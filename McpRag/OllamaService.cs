using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpRag;

/// <summary>
/// Сервис для взаимодействия с Ollama API, предоставляющий методы для работы с моделями,
/// генерации текста и эмбеддингов.
/// </summary>
public class OllamaService : IOllamaService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaConfig _config;
    private readonly ILogger<OllamaService> _logger;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="OllamaService"/>.
    /// </summary>
    /// <param name="httpClient">HTTP-клиент для отправки запросов к Ollama API.</param>
    /// <param name="config">Конфигурация Ollama, содержащая параметры подключения и модели.</param>
    /// <param name="logger">Логгер для записи информации о работе сервиса.</param>
    /// <exception cref="ArgumentNullException">Выбрасывается, если любой из параметров равен null.</exception>
    public OllamaService(HttpClient httpClient, IOptions<OllamaConfig> config, ILogger<OllamaService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
    }

    /// <summary>
    /// Проверяет доступность Ollama API.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>True, если API доступен; иначе false.</returns>
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

    /// <summary>
    /// Получает список доступных моделей в Ollama.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Список названий доступных моделей.</returns>
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

    /// <summary>
    /// Проверяет доступность конкретной модели в Ollama.
    /// </summary>
    /// <param name="modelName">Название модели для проверки.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>True, если модель доступна; иначе false.</returns>
    public async Task<bool> IsModelAvailableAsync(string modelName, CancellationToken cancellationToken = default)
    {
        var models = await ListModelsAsync(cancellationToken);
        return models.Any(m => m.StartsWith(modelName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Генерирует текстовый ответ на заданный промпт с использованием конфигурационной модели.
    /// </summary>
    /// <param name="prompt">Промпт для генерации ответа.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Сгенерированный текстовый ответ или сообщение об ошибке.</returns>
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

    /// <summary>
    /// Генерирует эмбеддинги для заданного текста с использованием конфигурационной модели для эмбеддингов.
    /// </summary>
    /// <param name="text">Текст для генерации эмбеддингов.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Массив значений эмбеддингов.</returns>
    /// <exception cref="HttpRequestException">Выбрасывается, если запрос к API завершился с ошибкой.</exception>
    /// <exception cref="InvalidOperationException">Выбрасывается, если API вернул пустой ответ.</exception>
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
