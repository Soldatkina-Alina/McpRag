Задача 10
Добавь в граф состояний узел GradeDocuments для оценки релевантности через LLM.

Требования:

1. РАСШИРЬ КОНФИГУРАЦИЮ RAGConfig:
   public class RAGConfig
   {
       // Существующие поля из Задачи 8-9
       public int MaxChunks { get; set; } = 5;
       public float MinRelevanceScore { get; set; } = 0.7f;
       public int MaxContextTokens { get; set; } = 2000;
       public bool IncludeMetadataInContext { get; set; } = true;
       
       // НОВЫЕ поля для GradeDocuments
       public GradeDocumentsConfig GradeDocuments { get; set; } = new();
   }
   
   public class GradeDocumentsConfig
   {
       public bool Enabled { get; set; } = true;           // вкл/выкл узел
       public float ScoreThreshold { get; set; } = 0.8f;   // выше этого - пропускаем оценку
       public float LLMThreshold { get; set; } = 0.5f;     // порог для LLM (0-1)
       public int MaxRetries { get; set; } = 1;            // повторы при ошибке
       public bool UseBinaryScore { get; set; } = true;    // true=yes/no, false=0-1
       public int BatchSize { get; set; } = 1;             // 1=по одному, >1=пакетно
   }

2. РАСШИРЬ DocumentChunk (добавь поля для оценки):
   public class DocumentChunk
   {
       // существующие поля
       public string Id { get; set; }
       public string Text { get; set; }
       public string Source { get; set; }
       public int ChunkIndex { get; set; }
       public float[] Embedding { get; set; }
       public DateTime IndexedAt { get; set; }
       public Dictionary<string, object> Metadata { get; set; } = new();
       
       // Score из ChromaDB (Задача 8)
       public float Score { get; set; }
       
       // НОВЫЕ поля для GradeDocuments
       public float? LLMScore { get; set; }           // оценка от LLM (null = не оценено)
       public bool IsRelevant { get; set; } = true;   // финальное решение
       public string GradeError { get; set; }         // ошибка при оценке (если была)
   }

3. РАСШИРЬ RagState:
   public class RagState
   {
       // существующие поля
       public string Question { get; set; }
       public List<DocumentChunk> Documents { get; set; } = new();
       public string Answer { get; set; }
       public List<ExecutionStep> ExecutionSteps { get; set; } = new();
       public bool HasError { get; set; }
       public string ErrorMessage { get; set; }
       
       // НОВЫЕ вспомогательные свойства
       public bool HasRelevantDocuments => 
           Documents.Any(d => d.IsRelevant && d.Score >= _config.MinRelevanceScore);
           
       public int RelevantCount => 
           Documents.Count(d => d.IsRelevant && d.Score >= _config.MinRelevanceScore);
   }

4. ДОБАВЬ УЗЕЛ GradeDocumentsNode В RagGraphService:

   private async Task<RagState> GradeDocumentsNodeAsync(RagState state, CancellationToken ct)
   {
       var step = new ExecutionStep { NodeName = "GradeDocuments" };
       var config = _config.Value.GradeDocuments;
       
       // Если узел отключен - просто возвращаем state
       if (!config.Enabled)
       {
           step.Metadata["skipped"] = true;
           state.ExecutionSteps.Add(step);
           return state;
       }
       
       var documentsToGrade = new List<DocumentChunk>();
       var skippedDocs = new List<DocumentChunk>();
       var errors = 0;
       
       try
       {
           // Разделяем документы на те, что нужно оценить, и те, что можно пропустить
           foreach (var doc in state.Documents)
           {
               // Если документ уже имеет высокий Score от ChromaDB - пропускаем оценку
               if (doc.Score >= config.ScoreThreshold)
               {
                   doc.IsRelevant = true;
                   doc.LLMScore = 1.0f; // считаем, что LLM бы тоже сказала "yes"
                   skippedDocs.Add(doc);
               }
               else
               {
                   documentsToGrade.Add(doc);
               }
           }
           
           step.Metadata["total_docs"] = state.Documents.Count;
           step.Metadata["skipped_docs"] = skippedDocs.Count;
           step.Metadata["to_grade"] = documentsToGrade.Count;
           
           // Оцениваем документы, которые требуют проверки
           if (documentsToGrade.Any())
           {
               if (config.BatchSize > 1)
               {
                   await GradeDocumentsBatchAsync(documentsToGrade, state.Question, config, ct);
               }
               else
               {
                   await GradeDocumentsSequentialAsync(documentsToGrade, state.Question, config, ct);
               }
               
               // Собираем статистику по ошибкам
               errors = documentsToGrade.Count(d => !string.IsNullOrEmpty(d.GradeError));
               
               // Обновляем IsRelevant на основе LLMScore
               foreach (var doc in documentsToGrade)
               {
                   doc.IsRelevant = doc.LLMScore >= config.LLMThreshold;
               }
           }
           
           // Формируем финальный список документов (только релевантные)
           state.Documents = state.Documents
               .Where(d => d.IsRelevant && d.Score >= _config.Value.MinRelevanceScore)
               .ToList();
           
           step.Metadata["relevant_after_grade"] = state.Documents.Count;
           step.Metadata["irrelevant"] = documentsToGrade.Count(d => !d.IsRelevant);
           step.Metadata["grade_errors"] = errors;
       }
       catch (Exception ex)
       {
           step.Metadata["error"] = ex.Message;
           _logger.LogError(ex, "GradeDocuments node failed");
           
           // При критической ошибке сохраняем все документы
           state.Documents = state.Documents
               .Where(d => d.Score >= _config.Value.MinRelevanceScore)
               .ToList();
           step.Metadata["fallback_to_score"] = true;
       }
       finally
       {
           state.ExecutionSteps.Add(step);
       }
       
       return state;
   }

5. РЕАЛИЗУЙ Sequential Grading (по одному):
   private async Task GradeDocumentsSequentialAsync(
       List<DocumentChunk> docs, 
       string question, 
       GradeDocumentsConfig config,
       CancellationToken ct)
   {
       foreach (var doc in docs)
       {
           try
           {
               doc.LLMScore = await GradeSingleDocumentAsync(doc, question, config, ct);
               doc.IsRelevant = doc.LLMScore >= config.LLMThreshold;
           }
           catch (Exception ex)
           {
               doc.GradeError = ex.Message;
               doc.LLMScore = null;
               doc.IsRelevant = true; // сохраняем при ошибке
               _logger.LogWarning(ex, "Failed to grade document {docId}", doc.Id);
           }
       }
   }

6. РЕАЛИЗУЙ GradeSingleDocumentAsync с надежным промптом:
   private async Task<float> GradeSingleDocumentAsync(
       DocumentChunk doc, 
       string question, 
       GradeDocumentsConfig config,
       CancellationToken ct)
   {
       string prompt;
       
       if (config.UseBinaryScore)
       {
           // Бинарная оценка (yes/no) - проще и быстрее
           prompt = $@"
Ты - эксперт по оценке релевантности документов для RAG системы.
Оцени, содержит ли документ информацию, полезную для ответа на вопрос.

Вопрос: {question}

Документ:
{doc.Text}

Инструкция: Ответь ТОЛЬКО одним словом: 'yes' если документ релевантен, 'no' если нет.
Не добавляй никаких пояснений, только 'yes' или 'no'.";
       }
       else
       {
           // Оценка по шкале 0-1 (более тонкая)
           prompt = $@"
Ты - эксперт по оценке релевантности документов для RAG системы.
Оцени по шкале от 0 до 1, насколько документ релевантен для ответа на вопрос.

Вопрос: {question}

Документ:
{doc.Text}

Инструкция: Ответь ТОЛЬКО числом от 0 до 1, где 0 = полностью нерелевантен, 1 = идеально релевантен.
Не добавляй никаких пояснений, только число.";
       }
       
       var response = await _ollama.GenerateAsync(prompt, ct);
       
       if (config.UseBinaryScore)
       {
           // Надежная обработка бинарного ответа
           var cleanResponse = response.Trim().ToLower();
           if (cleanResponse.StartsWith("y")) return 1.0f; // yes, yeap, yup
           if (cleanResponse.StartsWith("n")) return 0.0f; // no, nope
           
           // Если LLM вернула мусор - логируем и возвращаем 0.5 (нейтрально)
           _logger.LogWarning("Unexpected binary response: {response}", response);
           return 0.5f;
       }
       else
       {
           // Парсинг числа от 0 до 1
           if (float.TryParse(response.Trim(), out var score))
           {
               return Math.Clamp(score, 0, 1);
           }
           
           _logger.LogWarning("Expected float but got: {response}", response);
           return 0.5f; // нейтральное значение при ошибке
       }
   }

7. ОПЦИОНАЛЬНО: Batch Grading (для продвинутых):
   private async Task GradeDocumentsBatchAsync(
       List<DocumentChunk> docs, 
       string question, 
       GradeDocumentsConfig config,
       CancellationToken ct)
   {
       // Разбиваем на батчи по config.BatchSize
       for (int i = 0; i < docs.Count; i += config.BatchSize)
       {
           var batch = docs.Skip(i).Take(config.BatchSize).ToList();
           
           var prompt = $@"
Ты - эксперт по оценке релевантности документов.
Оцени каждый документ для вопроса: {question}

Документы:
{string.Join("\n---\n", batch.Select((d, idx) => $"[{idx}] {d.Text}"))}

Инструкция: Ответь списком чисел от 0 до 1 через запятую.
Пример: 0.9, 0.2, 0.7";

           var response = await _ollama.GenerateAsync(prompt, ct);
           
           // Парсим ответ (упрощенно)
           var scores = response.Split(',')
               .Select(s => float.TryParse(s.Trim(), out var f) ? f : 0.5f)
               .ToList();
               
           for (int j = 0; j < batch.Count && j < scores.Count; j++)
           {
               batch[j].LLMScore = scores[j];
               batch[j].IsRelevant = scores[j] >= config.LLMThreshold;
           }
       }
   }

8. ОБНОВИ ЛОГИКУ ГРАФА:
   // В ExecuteAsync после SearchNode добавляем:
   if (state.Documents.Any())
   {
       state = await GradeDocumentsNodeAsync(state, ct);
       
       if (!state.HasRelevantDocuments)
       {
           state.Answer = $"❌ После оценки релевантности не найдено подходящих документов.\n" +
                          $"Порог ChromaDB: {_config.Value.MinRelevanceScore:P1}, " +
                          $"порог LLM: {_config.Value.GradeDocuments.LLMThreshold:P1}";
           return state;
       }
   }
   
   // Затем GenerateNode (использует только state.Documents - уже отфильтрованные)

9. ОБНОВИ AskQuestionTool для отображения статистики GradeDocuments:
   // В формировании ответа добавить:
   if (state.ExecutionSteps.Any(s => s.NodeName == "GradeDocuments"))
   {
       var gradeStep = state.ExecutionSteps.First(s => s.NodeName == "GradeDocuments");
       result.AppendLine($"📊 **Оценка релевантности:** {gradeStep.Metadata["relevant_after_grade"]}/{gradeStep.Metadata["total_docs"]} документов");
       if (gradeStep.Metadata.ContainsKey("grade_errors"))
       {
           result.AppendLine($"   *Ошибок оценки: {gradeStep.Metadata["grade_errors"]}*");
       }
   }