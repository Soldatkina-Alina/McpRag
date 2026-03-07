Задача 13
Добавь оставшиеся три инструмента для завершения набора из 5.

Требования:

1. ИНСТРУМЕНТ find_relevant_docs:
   [McpServerTool]
   [Description("Находит релевантные документы по запросу без генерации ответа")]
   public async Task<string> FindRelevantDocs(
       [Description("Поисковый запрос")] string query,
       [Description("Количество результатов (по умолчанию 2)")] int topK = 2,
       [Description("Минимальная релевантность (0-1)")] double? minScore = 0.5,
       CancellationToken cancellationToken = default)
   {
       try
       {
           var actualMinScore = minScore ?? _config.MinRelevanceScore;
           
           // Поиск с релевантностью
           var results = await _vectorStore.SearchWithScoreAsync(query, topK, cancellationToken);
           
           if (!results.Any())
           {
               return "❌ Документы не найдены.";
           }
           
           // Фильтруем по порогу
           var relevantResults = results.Where(r => r.Score >= actualMinScore).ToList();
           
           if (!relevantResults.Any())
           {
               var maxScore = results.First().Score;
               return $"❌ Найдено {results.Count} документов, но все ниже порога {actualMinScore:P1}.\n" +
                      $"Максимальная релевантность: {maxScore:P1}";
           }
           
           var response = new StringBuilder();
           response.AppendLine($"📋 **Найдено {relevantResults.Count} релевантных документов:**");
           response.AppendLine();
           
           for (int i = 0; i < relevantResults.Count; i++)
           {
               var r = relevantResults[i];
               var chunk = r.Chunk;
               
               response.AppendLine($"**{i + 1}. {Path.GetFileName(chunk.Source)}** " +
                                  $"(релевантность: {r.Score:P1}, чанк {chunk.ChunkIndex})");
               response.AppendLine($"```\n{chunk.Text.Substring(0, Math.Min(300, chunk.Text.Length))}...\n```");
               response.AppendLine();
           }
           
           return response.ToString();
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "Error in FindRelevantDocs");
           return $"❌ Ошибка: {ex.Message}";
       }
   }

2. ИНСТРУМЕНТ summarize_document:
   [McpServerTool]
   [Description("Создает краткое содержание документа")]
   public async Task<string> SummarizeDocument(
       [Description("Путь к файлу для суммаризации")] string filePath,
       [Description("Максимальная длина саммари (в словах)")] int? maxWords = null,
       CancellationToken cancellationToken = default)
   {
       try
       {
           if (!File.Exists(filePath))
           {
               return $"❌ Файл '{filePath}' не существует.";
           }
           
           // Используем IndexerService из Задачи 6
           var chunks = await _indexer.LoadAndSplitDocumentAsync(filePath, cancellationToken);
           var fullText = string.Join("\n\n", chunks.Select(c => c.Text));
           
           // Базовая статистика
           var stats = new
           {
               FileName = Path.GetFileName(filePath),
               Size = new FileInfo(filePath).Length,
               Chunks = chunks.Count,
               Words = fullText.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length
           };
           
           // Определяем длину саммари
           var wordLimit = maxWords ?? Math.Max(50, stats.Words / 10);
           
           var prompt = $@"
Ты - эксперт по суммаризации документов.
Создай краткое содержание следующего документа.

Требования:
- Максимум {wordLimit} слов
- Выдели основные темы и ключевые моменты
- Сохрани важные детали
- Напиши на том же языке, что и документ

Документ:
{fullText.Substring(0, Math.Min(5000, fullText.Length))}
{(fullText.Length > 5000 ? "\n...[документ обрезан для суммаризации]" : "")}

Краткое содержание:";
           
           var summary = await _ollama.GenerateAsync(prompt, cancellationToken);
           
           var response = new StringBuilder();
           response.AppendLine($"📄 **Саммари документа: {stats.FileName}**");
           response.AppendLine();
           response.AppendLine(summary);
           response.AppendLine();
           response.AppendLine("---");
           response.AppendLine($"📊 **Статистика:**");
           response.AppendLine($"- Размер файла: {stats.Size / 1024.0:F1} KB");
           response.AppendLine($"- Всего слов: {stats.Words}");
           response.AppendLine($"- Чанков: {stats.Chunks}");
           response.AppendLine($"- Длина саммари: {summary.Split().Length} слов");
           
           return response.ToString();
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "Error in SummarizeDocument");
           return $"❌ Ошибка: {ex.Message}";
       }
   }

3. ИНСТРУМЕНТ index_status:
   [McpServerTool]
   [Description("Показывает статистику индекса")]
   public async Task<string> IndexStatus(CancellationToken cancellationToken = default)
   {
       try
       {
           var stats = await _vectorStore.GetStatisticsAsync(cancellationToken);
           
           var response = new StringBuilder();
           response.AppendLine("📊 **Статус индекса:**");
           response.AppendLine();
           
           if (stats.TotalChunks == 0)
           {
               response.AppendLine("❌ Индекс пуст. Выполните `index_folder` для индексации документов.");
               return response.ToString();
           }
           
           response.AppendLine($"📁 **Всего файлов:** {stats.TotalFiles}");
           response.AppendLine($"📄 **Всего чанков:** {stats.TotalChunks}");
           
           if (stats.LastIndexed.HasValue)
           {
               response.AppendLine($"🕒 **Последняя индексация:** {stats.LastIndexed.Value:yyyy-MM-dd HH:mm:ss}");
               response.AppendLine($"⏱️ **Прошло:** {(DateTime.UtcNow - stats.LastIndexed.Value).TotalHours:F1} часов");
           }
           
           // Дополнительная информация, если доступна
           if (stats.Collections != null && stats.Collections.Any())
           {
               response.AppendLine();
               response.AppendLine("🗂️ **Коллекции ChromaDB:**");
               foreach (var collection in stats.Collections)
               {
                   response.AppendLine($"- {collection.Name}: {collection.Count} документов");
               }
           }
           
           return response.ToString();
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "Error in IndexStatus");
           return $"❌ Ошибка при получении статуса: {ex.Message}";
       }
   }

4. РАСШИРЬ IVectorStoreService для статистики:
   public class IndexStatistics
   {
       public int TotalChunks { get; set; }
       public int TotalFiles { get; set; }
       public DateTime? LastIndexed { get; set; }
       public List<CollectionInfo> Collections { get; set; } = new();
   }
   
   public class CollectionInfo
   {
       public string Name { get; set; }
       public int Count { get; set; }
       public DateTime Created { get; set; }
   }

5. ЗАРЕГИСТРИРУЙ ВСЕ ИНСТРУМЕНТЫ В DI:
   builder.Services.AddMcpTool<IndexFolderTool>();
   builder.Services.AddMcpTool<AskQuestionTool>();
   builder.Services.AddMcpTool<FindRelevantDocsTool>();
   builder.Services.AddMcpTool<SummarizeDocumentTool>();
   builder.Services.AddMcpTool<IndexStatusTool>();