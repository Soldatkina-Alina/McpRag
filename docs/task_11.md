Задача 11
Добавь в граф состояний retry-цикл с переписыванием и расширением запроса.

Требования:

1. РАСШИРЬ КОНФИГУРАЦИЮ RAGConfig:
   public class RAGConfig
   {
       // существующие поля
       public int MaxChunks { get; set; } = 5;
       public float MinRelevanceScore { get; set; } = 0.7f;
       
       // НОВЫЕ поля для Retry-цикла
       public RetryConfig Retry { get; set; } = new();
   }
   
   public class RetryConfig
   {
       public int MaxRetries { get; set; } = 2;           // максимум попыток
       public int MinRelevantCount { get; set; } = 2;     // сколько релевантных нужно
       public bool EnableQueryRewrite { get; set; } = true; // переписывать перед первым поиском
       public float ScoreBoostPerRetry { get; set; } = 0.1f; // снижение порога при retry
   }

2. РАСШИРЬ RagState:
   public class RagState
   {
       // существующие поля
       public string Question { get; set; }
       public List<DocumentChunk> Documents { get; set; } = new();
       public string Answer { get; set; }
       public List<ExecutionStep> ExecutionSteps { get; set; } = new();
       
       // НОВЫЕ поля для Retry
       public string CurrentQuery { get; set; }           // текущий поисковый запрос
       public List<string> QueryHistory { get; set; } = new(); // история запросов
       public int RetryCount { get; set; }
       public float CurrentScoreThreshold { get; set; }   // динамический порог
   }

3. ДОБАВЬ УЗЕЛ RewriteQuery:
   private async Task<RagState> RewriteQueryNodeAsync(RagState state, CancellationToken ct)
   {
       var step = new ExecutionStep { NodeName = "RewriteQuery" };
       
       try
       {
           var prompt = $@"
Ты - эксперт по улучшению поисковых запросов для RAG системы.
Исходный запрос пользователя: {state.Question}

Твоя задача: перепиши запрос так, чтобы он лучше подходил для поиска релевантных документов.
- Используй более точные термины
- Добавь важные ключевые слова
- Сохрани исходный смысл
- Ответь ТОЛЬКО улучшенным запросом, без пояснений

Улучшенный запрос:";
           
           var rewritten = await _ollama.GenerateAsync(prompt, ct);
           state.CurrentQuery = rewritten.Trim();
           state.QueryHistory.Add(rewritten.Trim());
           
           step.Metadata["original"] = state.Question;
           step.Metadata["rewritten"] = state.CurrentQuery;
       }
       catch (Exception ex)
       {
           step.Metadata["error"] = ex.Message;
           state.CurrentQuery = state.Question; // fallback к исходному
       }
       finally
       {
           state.ExecutionSteps.Add(step);
       }
       
       return state;
   }

4. ДОБАВЬ УЗЕЛ BroadenQuery:
   private async Task<RagState> BroadenQueryNodeAsync(RagState state, CancellationToken ct)
   {
       var step = new ExecutionStep { NodeName = "BroadenQuery" };
       
       try
       {
           var prompt = $@"
Ты - эксперт по расширению поисковых запросов.
Текущий запрос: {state.CurrentQuery}

Найдено недостаточно релевантных документов.
Расширь запрос, чтобы найти больше информации:
- Добавь синонимы
- Используй более общие термины
- Включи связанные понятия

Ответь ТОЛЬКО расширенным запросом, без пояснений.

Расширенный запрос:";
           
           var broadened = await _ollama.GenerateAsync(prompt, ct);
           state.CurrentQuery = broadened.Trim();
           state.QueryHistory.Add(broadened.Trim());
           state.RetryCount++;
           
           // Динамически снижаем порог релевантности при каждой попытке
           state.CurrentScoreThreshold = _config.Value.MinRelevanceScore * 
                                         (1 - _config.Value.Retry.ScoreBoostPerRetry * state.RetryCount);
           
           step.Metadata["previous_query"] = state.QueryHistory[^2];
           step.Metadata["broadened_query"] = state.CurrentQuery;
           step.Metadata["retry_count"] = state.RetryCount;
           step.Metadata["new_threshold"] = state.CurrentScoreThreshold;
       }
       catch (Exception ex)
       {
           step.Metadata["error"] = ex.Message;
           // при ошибке оставляем тот же запрос
       }
       finally
       {
           state.ExecutionSteps.Add(step);
       }
       
       return state;
   }

5. ДОБАВЬ МЕТОД ПРОВЕРКИ ДОСТАТОЧНОСТИ:
   private bool HasEnoughRelevantDocuments(RagState state)
   {
       var config = _config.Value.Retry;
       
       // Используем динамический порог и счетчик релевантных
       var relevantCount = state.Documents.Count(d => 
           d.IsRelevant && d.Score >= state.CurrentScoreThreshold);
       
       return relevantCount >= config.MinRelevantCount;
   }

6. ОБНОВИ ЛОГИКУ ГРАФА:
   public async Task<RagState> ExecuteAsync(string question, CancellationToken ct)
   {
       var state = new RagState 
       { 
           Question = question,
           CurrentQuery = question,
           CurrentScoreThreshold = _config.Value.MinRelevanceScore
       };
       
       try
       {
           // Шаг 1: Rewrite (улучшаем запрос перед первым поиском)
           if (_config.Value.Retry.EnableQueryRewrite)
           {
               state = await RewriteQueryNodeAsync(state, ct);
           }
           
           // Начало retry-цикла
           while (state.RetryCount <= _config.Value.Retry.MaxRetries)
           {
               // Шаг 2: Search с текущим запросом
               state = await SearchNodeAsync(state, ct);
               
               // Шаг 3: GradeDocuments (из Задачи 10)
               state = await GradeDocumentsNodeAsync(state, ct);
               
               // Проверка: достаточно ли документов?
               if (HasEnoughRelevantDocuments(state))
               {
                   break; // достаточно - выходим из цикла
               }
               
               // Если есть еще попытки - расширяем запрос
               if (state.RetryCount < _config.Value.Retry.MaxRetries)
               {
                   state = await BroadenQueryNodeAsync(state, ct);
                   // Продолжаем цикл
               }
               else
               {
                   // Достигнут лимит попыток
                   state.ExecutionSteps.Add(new ExecutionStep 
                   { 
                       NodeName = "MaxRetriesReached",
                       Metadata = new Dictionary<string, object>
                       {
                           ["max_retries"] = _config.Value.Retry.MaxRetries,
                           ["relevant_found"] = state.Documents.Count(d => d.IsRelevant)
                       }
                   });
                   break;
               }
           }
           
           // Если после всех попыток нет релевантных документов
           if (!state.Documents.Any(d => d.IsRelevant))
           {
               state.Answer = $"❌ После {state.RetryCount + 1} попыток не найдено релевантных документов.\n" +
                              $"Последний запрос: '{state.CurrentQuery}'\n" +
                              $"История запросов: {string.Join(" → ", state.QueryHistory)}";
               return state;
           }
           
           // Шаг 4: Generate (из Задачи 9)
           state = await GenerateNodeAsync(state, ct);
           
           // Шаг 5: HallucinationCheck (будет в Задаче 12)
           // ...
           
           return state;
       }
       catch (Exception ex)
       {
           // обработка ошибок
           state.HasError = true;
           state.ErrorMessage = ex.Message;
           return state;
       }
   }

7. ОБНОВИ SearchNodeAsync для использования CurrentQuery:
   private async Task<RagState> SearchNodeAsync(RagState state, CancellationToken ct)
   {
       var step = new ExecutionStep { NodeName = "Search" };
       
       try
       {
           // Используем текущий запрос (может быть переписан или расширен)
           var results = await _vectorStore.SearchWithScoreAsync(
               state.CurrentQuery ?? state.Question,
               _config.Value.MaxChunks,
               ct);
           
           // Обновляем документы (но сохраняем предыдущие? нет - начинаем с нуля)
           state.Documents = results.Select(r => 
           {
               r.Chunk.Score = r.Score;
               return r.Chunk;
           }).ToList();
           
           step.Metadata["query_used"] = state.CurrentQuery;
           step.Metadata["chunks_found"] = state.Documents.Count;
           step.Metadata["retry_count"] = state.RetryCount;
       }
       catch (Exception ex)
       {
           step.Metadata["error"] = ex.Message;
           throw;
       }
       finally
       {
           state.ExecutionSteps.Add(step);
       }
       
       return state;
   }

8. ОБНОВИ AskQuestionTool для отображения retry-информации:
   // В формировании ответа добавить:
   if (state.QueryHistory.Any())
   {
       result.AppendLine("📝 **История запросов:**");
       for (int i = 0; i < state.QueryHistory.Count; i++)
       {
           result.AppendLine($"  {i + 1}. {state.QueryHistory[i]}");
       }
   }