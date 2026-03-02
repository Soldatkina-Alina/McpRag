using System;
using System.Collections.Generic;

namespace McpRag;

/// <summary>
/// Представляет шаг выполнения графа RAG.
/// </summary>
public class ExecutionStep
{
    /// <summary>
    /// Имя узла графа.
    /// </summary>
    public string NodeName { get; set; }

    /// <summary>
    /// Временная метка выполнения шага.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Метаданные о выполнении шага (например, количество найденных чанков, релевантность и т.д.).
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}