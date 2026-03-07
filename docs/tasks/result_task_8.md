# Результат Задачи 8

## Что было сделано

1. **Создан класс конфигурации RAGConfig**: Для хранения настроек RAG (Retrieval-Augmented Generation)
2. **Создан ContextFormatter**: Класс для форматирования контекста документов в понятный формат
3. **Обновлен IVectorStoreService**: Добавлен интерфейс для поиска с релевантностью
4. **Обновлен ChromaDbService**: Реализован метод SearchWithScoreAsync для возвращения релевантности
5. **Создан инструмент AskQuestionTool**: Инструмент для получения ответов из базы знаний с использованием RAG
6. **Обновлен appsettings.json**: Добавлена секция конфигурации для RAG
7. **Обновлен Program.cs**: Добавлена регистрация сервисов для RAG

## Функционал инструмента ask_question

Инструмент предоставляет следующую функциональность:
- Принимает вопрос и параметры для поиска (минимальная релевантность, максимальное количество чанков)
- Ищет релевантные документы в ChromaDB с учетом порога релевантности
- Форматирует контекст документов для отправки в LLM
- Отправляет запрос в LLM и возвращает ответ с ссылками на источники
- Обрабатывает ошибки и логирует работу

## Конфигурационные файлы

### RAGConfig.cs
```csharp
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
```

### ContextFormatter.cs
```csharp
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
```

### AskQuestionTool.cs
```csharp
namespace McpRag.Tools
{
    /// <summary>
    /// Инструмент для получения ответов на вопросы из базы знаний с использованием RAG.
    /// </summary>
    internal class AskQuestionTool
    {
        // ... реализация
    }
}
```

### appsettings.json
```json
{
  "RAG": {
    "MaxChunks": 5,
    "MinRelevanceScore": 0.7,
    "MaxContextTokens": 2000,
    "IncludeMetadataInContext": true
  }
}
```

## Проверка результат

Все тесты прошли успешно. Инструмент ask_question работает корректно и возвращает ответы с ссылками на источники.

## Пример использования

```json
{
  "server_name": "McpRag",
  "tool_name": "ask_question",
  "arguments": {
    "question": "Объясни асинхронное программирование в C#",
    "minScore": 0.7,
    "maxChunks": 5
  }
}
```

**Результат**: Включает ответ с объяснением асинхронного программирования и ссылками на источники.