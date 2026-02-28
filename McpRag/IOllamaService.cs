using System.Threading;
using System.Threading.Tasks;

namespace McpRag;

/// <summary>
/// Interface for Ollama service operations.
/// </summary>
public interface IOllamaService
{
    /// <summary>
    /// Checks if Ollama is available.
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets list of available models.
    /// </summary>
    Task<List<string>> ListModelsAsync(CancellationToken ct = default);

    /// <summary>
    /// Checks if specific model is available.
    /// </summary>
    Task<bool> IsModelAvailableAsync(string modelName, CancellationToken ct = default);
}