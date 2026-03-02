using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McpRag
{
    /// <summary>
    /// Конфигурация для RAG (Retrieval-Augmented Generation).
    /// </summary>
    public class RAGConfig
    {
        /// <summary>
        /// Максимальное количество чанков для поиска.
        /// </summary>
        public int MaxChunks { get; set; } = 5;

        /// <summary>
        /// Минимальный порог релевантности (от 0 до 1).
        /// </summary>
        public double MinRelevanceScore { get; set; } = 0.7;

        /// <summary>
        /// Максимальное количество токенов в контексте.
        /// </summary>
        public int MaxContextTokens { get; set; } = 2000;

        /// <summary>
        /// Включать ли метаданные в контекст.
        /// </summary>
        public bool IncludeMetadataInContext { get; set; } = true;
    }
}