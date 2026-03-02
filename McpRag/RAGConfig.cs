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

        /// <summary>
        /// Конфигурация для узла оценки документов.
        /// </summary>
        public GradeDocumentsConfig GradeDocuments { get; set; } = new();
    }

    /// <summary>
    /// Конфигурация для узла оценки документов через LLM.
    /// </summary>
    public class GradeDocumentsConfig
    {
        /// <summary>
        /// Включить/выключить узел оценки документов.
        /// </summary>
        public bool Enabled { get; set; } = false; // Выключен по умолчанию для тестирования

        /// <summary>
        /// Порог релевантности, выше которого оценка пропускается (от 0 до 1).
        /// </summary>
        public float ScoreThreshold { get; set; } = 0.6f; // Более лояльный порог

        /// <summary>
        /// Порог релевантности для LLM оценки (от 0 до 1).
        /// </summary>
        public float LLMThreshold { get; set; } = 0.4f; // Более лояльный порог

        /// <summary>
        /// Максимальное количество повторений при ошибке оценки.
        /// </summary>
        public int MaxRetries { get; set; } = 1;

        /// <summary>
        /// Использовать бинарную оценку (yes/no) вместо числовой (0-1).
        /// </summary>
        public bool UseBinaryScore { get; set; } = true;

        /// <summary>
        /// Размер батча для пакетной оценки документов.
        /// </summary>
        public int BatchSize { get; set; } = 1;
    }
}
