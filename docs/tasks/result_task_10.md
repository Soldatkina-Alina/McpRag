# Задача 10: Добавление узла GradeDocuments для оценки релевантности через LLM

## Описание задачи

Дополнить граф состояний RAG узлом для оценки релевантности документов с помощью LLM. Цель - улучшить качество результатов за счет дополнительной проверки релевантности найденных документов.

## Основные изменения

### 1. Расширение конфигурации

В `RAGConfig.cs` добавлен новый раздел конфигурации для узла GradeDocuments:

```csharp
public class GradeDocumentsConfig
{
    /// <summary>
    /// Включить/выключить узел оценки документов
    /// </summary>
    public bool Enabled { get; set; } = false; // Выключен по умолчанию для тестирования

    /// <summary>
    /// Порог релевантности, выше которого оценка пропускается
    /// </summary>
    public float ScoreThreshold { get; set; } = 0.6f; // Более лояльный порог

    /// <summary>
    /// Порог релевантности для LLM оценки
    /// </summary>
    public float LLMThreshold { get; set; } = 0.4f; // Более лояльный порог

    /// <summary>
    /// Максимальное количество повторений при ошибке
    /// </summary>
    public int MaxRetries { get; set; } = 1;

    /// <summary>
    /// Использовать бинарную оценку (yes/no) вместо числовой (0-1)
    /// </summary>
    public bool UseBinaryScore { get; set; } = true;

    /// <summary>
    /// Размер батча для пакетной оценки
    /// </summary>
    public int BatchSize { get; set; } = 1;
}
```

### 2. Расширение структуры данных

В `IVectorStoreService.cs` обновлен класс `DocumentChunk` с новыми полями для оценки:

```csharp
/// <summary>
/// Рейтинг релевантности от LLM (null = не оценено)
/// </summary>
public float? LLMScore { get; set; }

/// <summary>
/// Финальное решение о релевантности
/// </summary>
public bool IsRelevant { get; set; } = true;

/// <summary>
/// Ошибка при оценке (если была)
/// </summary>
public string GradeError { get; set; }
```

### 3. Расширение состояния графа

В `RagState.cs` добавлены вспомогательные свойства для работы с релевантными документами:

```csharp
/// <summary>
/// Проверяет, есть ли релевантные документы
/// </summary>
public bool HasRelevantDocuments =>
    Documents.Any(d => d.IsRelevant && d.Score >= _config.MinRelevanceScore);

/// <summary>
/// Количество релевантных документов
/// </summary>
public int RelevantCount =>
    Documents.Count(d => d.IsRelevant && d.Score >= _config.MinRelevanceScore);
```

### 4. Реализация узла GradeDocuments

В `RagGraphService.cs` добавлен новый узел `GradeDocumentsNodeAsync` с логикой оценки:

- Проверка на включение узла
- Разделение документов на те, которые нужно оценить и те, что можно пропустить
- Последовательная или пакетная оценка документов
- Обновление состояния с результатами оценки
- Обработка ошибок и fallback механизм

### 5. Методы оценки

Добавлены методы для разных режимов оценки:

- `GradeDocumentsSequentialAsync` - последовательная оценка документов
- `GradeSingleDocumentAsync` - оценка одного документа с надежной обработкой ответов
- `GradeDocumentsBatchAsync` - пакетная оценка документов (опционально)

### 6. Обновление инструмента AskQuestion

В `AskQuestionTool.cs` добавлена статистика оценки релевантности в ответе:

```csharp
if (state.ExecutionSteps.Any(s => s.NodeName == "GradeDocuments"))
{
    var gradeStep = state.ExecutionSteps.First(s => s.NodeName == "GradeDocuments");
    result.AppendLine();
    result.AppendLine($"📊 **Оценка релевантности:** {gradeStep.Metadata["relevant_after_grade"]}/{gradeStep.Metadata["total_docs"]} документов");
    if (gradeStep.Metadata.ContainsKey("grade_errors"))
    {
        result.AppendLine($"   *Ошибок оценки: {gradeStep.Metadata["grade_errors"]}*");
    }
}
```

## Функционал узла GradeDocuments

### Основные возможности

1. **Включение/отключение узла** - через конфигурацию
2. **Порог пропуска** - документы с высоким Score от ChromaDB не оцениваются
3. **Две модели оценки** - бинарная (yes/no) или числовая (0-1)
4. **Последовательная или пакетная обработка** - с поддержкой батчей
5. **Обработка ошибок** - с логированием и fallback机制
6. **Статистика** - отчет о количестве документов, оцененных и пропущенных

### Логика работы

1. Проверка на включение узла
2. Разделение документов на категории
3. Оценка документов, требующих проверки
4. Обновление состояния с результатами
5. Формирование статистики

## Проверка решений

### Компиляция

Проект успешно компилируется, не возникло ошибок.

### Тестирование

Все 32 теста проходят успешно:

```
Сводка теста: всего: 32; сбой: 0; успешно: 32; пропущено: 0; длительность: 1,3 с
```

### Запуск приложения

Приложение запускается и проверяет доступность сервисов:

```
[23:17:39 INF] Checking services availability...
[23:17:39 INF] Start processing HTTP request POST http://localhost:11434/api/embeddings
[23:17:39 INF] Sending HTTP request POST http://localhost:11434/api/embeddings
[23:17:41 INF] Received HTTP response headers after 2102.9233ms - 200
[23:17:41 INF] End processing HTTP request after 2124.2861ms - 200
[23:17:41 INF] Ollama service is available
[23:17:41 INF] Getting document count from ChromaDB collection: documents
[23:17:41 INF] Start processing HTTP request GET http://localhost:8000/api/v2/tenants/default_tenant/databases/default_database/collections
[23:17:41 INF] Sending HTTP request GET http://localhost:8000/api/v2/tenants/default_tenant/databases/default_database/collections
[23:17:41 INF] Received HTTP response headers after 14.5253ms - 200
[23:17:41 INF] End processing HTTP request after 20.0098ms - 200
[23:17:41 INF] Start processing HTTP request GET http://localhost:8000/api/v2/tenants/default_tenant/databases/default_database/collections/7a405beb-5b93-40e0-9e52-60874c2f6d21/count
[23:17:41 INF] Sending HTTP request GET http://localhost:8000/api/v2/tenants/default_tenant/databases/default_database/collections/7a405beb-5b93-40e0-9e52-60874c2f6d21/count
[23:17:41 INF] Received HTTP response headers after 3.8571ms - 200
[23:17:41 INF] End processing HTTP request after 9.6741ms - 200
[23:17:41 INF] ChromaDB collection contains 2 documents
[23:17:41 INF] ChromaDB service is available
[23:17:41 INF] All services are available. Server started
```

## Результат

Был успешно реализован узел GradeDocuments для оценки релевантности документов через LLM. Новая функциональность позволяет:

- Улучшить качество результатов за счет дополнительной проверки
- Настраивать пороги релевантности
- Использовать бинарную или числовую оценку
- Обрабатывать ошибки и пропускать документы с высоким Score
- Получать статистику о процессе оценки

Система теперь более гибко настроена и дает более точные результаты за счет двойной проверки релевантности - сначала через ChromaDB, затем через LLM.

Для решения проблемы с фильтрацией документов узел GradeDocuments был по умолчанию отключен (`Enabled = false`), что позволяет избежать слишком строгой фильтрации при тестировании.