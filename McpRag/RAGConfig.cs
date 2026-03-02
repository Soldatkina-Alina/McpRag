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
        /// Максимальное количество фрагментов документов для включения в контекст.
        /// </summary>
        public int MaxChunks { get; set; } = 5;

        /// <summary>
        /// Минимальный порог релевантности для включения в контекст.
        /// </summary>
        public float MinRelevanceScore { get; set; } = 0.7f;

        /// <summary>
        /// Максимальное количество токенов в контексте.
        /// </summary>
        public int MaxContextTokens { get; set; } = 2000;

        /// <summary>
        /// Включать ли метаданные в контекст.
        /// </summary>
        public bool IncludeMetadataInContext { get; set; } = true;

        /// <summary>
        /// Конфигурация для оценки документов через LLM.
        /// </summary>
        public GradeDocumentsConfig GradeDocuments { get; set; } = new();

        /// <summary>
        /// Конфигурация для retry-цикла с переписыванием и расширением запроса.
        /// </summary>
        public RetryConfig Retry { get; set; } = new();
    }

    /// <summary>
    /// Конфигурация для retry-цикла.
    /// </summary>
    public class RetryConfig
    {
        /// <summary>
        /// Максимальное количество попыток поиска.
        /// </summary>
        public int MaxRetries { get; set; } = 2;

        /// <summary>
        /// Минимальное количество релевантных документов, необходимое для успешного поиска.
        /// </summary>
        public int MinRelevantCount { get; set; } = 2;

        /// <summary>
        /// Включить или отключить автоматическое переписывание запроса перед первым поиском.
        /// </summary>
        public bool EnableQueryRewrite { get; set; } = true;

        /// <summary>
        /// Снижение порога релевантности при каждой дополнительной попытке.
        /// </summary>
        public float ScoreBoostPerRetry { get; set; } = 0.1f;
    }

    /// <summary>
    /// Конфигурация для оценки документов через LLM.
    /// </summary>
    public class GradeDocumentsConfig
    {
        /// <summary>
        /// Включить или отключить оценку документов через LLM.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Порог Score от ChromaDB для пропуска оценки LLM.
        /// </summary>
        public float ScoreThreshold { get; set; } = 0.9f;

        /// <summary>
        /// Порог оценки LLM для определения релевантности.
        /// </summary>
        public float LLMThreshold { get; set; } = 0.7f;

        /// <summary>
        /// Использовать бинарную оценку (yes/no) вместо вещественного числа.
        /// </summary>
        public bool UseBinaryScore { get; set; } = true;

        /// <summary>
        /// Размер батча для пакетной обработки.
        /// </summary>
        public int BatchSize { get; set; } = 1;
    }
}