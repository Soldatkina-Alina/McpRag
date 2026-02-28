using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace McpRag.Tools;

public class CheckOllamaTool
{
    private readonly IOllamaService _ollamaService;
    private readonly OllamaConfig _config;
    private readonly ILogger<CheckOllamaTool> _logger;

    public CheckOllamaTool(IOllamaService ollamaService, IOptions<OllamaConfig> config, ILogger<CheckOllamaTool> logger)
    {
        _ollamaService = ollamaService;
        _config = config.Value;
        _logger = logger;
    }

    [McpServerTool]
    [Description("Checks the health of Ollama service and verifies required models are available.")]
    public async Task<string> CheckOllama()
    {
        _logger.LogInformation("CheckOllama tool called");

        try
        {
            // Check if Ollama is available
            var isHealthy = await _ollamaService.IsHealthyAsync();
            if (!isHealthy)
            {
                return "❌ Ollama не доступна. Убедитесь, что она запущена (ollama serve)";
            }

            var result = $"✅ Ollama доступна по адресу {_config.BaseUrl}\n";

            // Get all available models
            var models = await _ollamaService.ListModelsAsync();
            if (models.Count > 0)
            {
                result += $"📋 Доступные модели: {string.Join(", ", models)}\n";
            }
            else
            {
                result += "⚠️ Нет доступных моделей. Установите нужные модели: ollama pull phi3:mini и ollama pull nomic-embed-text\n";
            }

            // Check required models
            var requiredModels = new[] { _config.Model, _config.EmbeddingModel };
            var missingModels = new List<string>();

            foreach (var model in requiredModels)
            {
                var isAvailable = await _ollamaService.IsModelAvailableAsync(model);
                if (!isAvailable)
                {
                    missingModels.Add(model);
                }
            }

            if (missingModels.Count == 0)
            {
                result += $"✅ Нужные модели установлены: {string.Join(", ", requiredModels)}";
            }
            else
            {
                result += $"⚠️ Отсутствуют модели: {string.Join(", ", missingModels)}. Установи: ollama pull {string.Join(" && ollama pull ", missingModels)}";
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Ollama: {Message}", ex.Message);
            
            if (ex.Message.Contains("Connection refused"))
            {
                return "❌ Ollama не запущена. Запустите: ollama serve";
            }
            if (ex.Message.Contains("timed out"))
            {
                return "⏱️ Превышено время ожидания. Проверьте соединение с Ollama";
            }
            
            return $"❌ Ошибка: {ex.Message}";
        }
    }
}