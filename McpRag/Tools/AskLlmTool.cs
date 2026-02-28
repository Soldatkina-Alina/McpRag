using System.ComponentModel;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpRag.Tools;

/// <summary>
/// Tool for asking questions to LLM directly.
/// </summary>
internal class AskLlmTool
{
    private readonly IOllamaService _ollamaService;
    private readonly OllamaConfig _config;
    private readonly ILogger<AskLlmTool> _logger;

    public AskLlmTool(IOllamaService ollamaService, IOptions<OllamaConfig> config, ILogger<AskLlmTool> logger)
    {
        _ollamaService = ollamaService;
        _config = config.Value;
        _logger = logger;
    }

    [McpServerTool]
    [Description("Asks a question to LLM directly without RAG.")]
    public async Task<string> AskLlm(
        [Description("Question to ask LLM")] string question)
    {
        _logger.LogInformation("AskLlm tool called with question: {Question}", question);

        try
        {
            // Check if Ollama is available
            if (!await _ollamaService.IsHealthyAsync())
            {
                _logger.LogWarning("Ollama is not available");
                return "❌ Ollama не доступна. Запустите ollama serve";
            }

            // Check if model is available
            if (!await _ollamaService.IsModelAvailableAsync(_config.Model))
            {
                _logger.LogWarning("Model {Model} is not available", _config.Model);
                return $"❌ Модель {_config.Model} не найдена. Установите: ollama pull {_config.Model}";
            }

            // Generate response
            var response = await _ollamaService.GenerateAsync(question);
            return response;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("AskLlm operation was canceled");
            return "⏱️ Превышено время ожидания ответа от Ollama";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AskLlm tool: {Message}", ex.Message);
            return "❌ Произошла ошибка при запросе к LLM. Проверьте логи для деталей";
        }
    }
}