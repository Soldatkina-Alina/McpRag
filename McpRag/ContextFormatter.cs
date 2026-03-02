using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace McpRag
{
    /// <summary>
    /// Форматирует контекст для RAG.
    /// </summary>
    public class ContextFormatter
    {
        /// <summary>
        /// Форматирует список чанков документов для использования в контексте.
        /// </summary>
        /// <param name="chunks">Список чанков документов.</param>
        /// <param name="includeMetadata">Включать ли метаданные.</param>
        /// <returns>Форматированный контекст.</returns>
        public string FormatContext(List<DocumentChunk> chunks, bool includeMetadata = true)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                sb.AppendLine($"--- [Источник {i+1}]: {System.IO.Path.GetFileName(chunk.Source)} ---");
                if (includeMetadata)
                {
                    sb.AppendLine($"   (чанк {chunk.ChunkIndex}, релевантность: {chunk.Score:P1})");
                }
                sb.AppendLine(chunk.Text);
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}