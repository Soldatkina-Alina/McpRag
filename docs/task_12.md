Задача 12
Добавь в граф проверку на галлюцинации и регенерацию ответа.

Требования:

1. РАСШИРЬ КОНФИГУРАЦИЮ RAGConfig:
   public class RAGConfig
   {
       // существующие поля
       public RetryConfig Retry { get; set; } = new();
       
       // НОВЫЕ поля для HallucinationCheck
       public HallucinationConfig Hallucination { get; set; } = new();
   }
   
   public class HallucinationConfig
   {
       public bool Enabled { get; set; } = true;
       public int MaxRegenerations { get; set; } = 1;      // максимум регенераций
       public float ConfidenceThreshold { get; set; } = 0.7f; // порог уверенности
       public bool UseDetailedCheck { get; set; } = false;  // детальная проверка по предложениям
   }

2. РАСШИРЬ RagState:
   public class RagState
   {
       // существующие поля
       public string Answer { get; set; }
       public List<ExecutionStep> ExecutionSteps { get; set; } = new();
       
       // НОВЫЕ поля для HallucinationCheck
       public bool IsGrounded { get; set; }
       public int RegenerationCount { get; set; }
       public List<string> AnswerHistory { get; set; } = new(); // история ответов
       public float? GroundingScore { get; set; }          // 0-1 насколько grounded
   }

3. ДОБАВЬ УЗЕЛ HallucinationCheck:
   private async Task<RagState> HallucinationCheckNodeAsync(RagState state, CancellationToken ct)
   {
       var step = new ExecutionStep { NodeName = "HallucinationCheck" };
       var config = _config.Value.Hallucination;
       
       if (!config.Enabled || string.IsNullOrEmpty(state.Answer))
       {
           step.Metadata["skipped"] = true;
           state.ExecutionSteps.Add(step);
           return state;
       }
       
       try
       {
           // Формируем контекст из релевантных документов
           var context = string.Join("\n\n", state.Documents
               .Where(d => d.IsRelevant)
               .Select(d => d.Text));
           
           // Промпт для проверки
           var prompt = $@"
Ты - эксперт по проверке фактов в RAG системах.
Проверь, основан ли ответ только на предоставленном контексте.

Контекст (документы):
{context}

Ответ системы:
{state.Answer}

Вопрос пользователя: {state.Question}

Инструкция: Оцени, содержит ли ответ информацию, которой нет в контексте.
Ответь в формате JSON:
{{
  ""is_grounded"": true/false,  // true если ответ полностью основан на контексте
  ""confidence"": 0.0-1.0,      // насколько ты уверен в оценке
  ""hallucinated_parts"": [      // если есть выдумки, укажи их
    ""фраза 1"",
    ""фраза 2""
  ],
  ""explanation"": ""краткое объяснение""
}}";
           
           var response = await _ollama.GenerateAsync(prompt, ct);
           
           // Парсим JSON ответ (упрощенно - лучше использовать System.Text.Json)
           try
           {
               var result = JsonSerializer.Deserialize<HallucinationResult>(response);
               state.IsGrounded = result.is_grounded;
               state.GroundingScore = result.confidence;
               
               step.Metadata["is_grounded"] = state.IsGrounded;
               step.Metadata["confidence"] = result.confidence;
               step.Metadata["hallucinated_parts"] = result.hallucinated_parts?.Count ?? 0;
               
               if (!state.IsGrounded && result.hallucinated_parts != null)
               {
                   step.Metadata["examples"] = string.Join("; ", result.hallucinated_parts.Take(3));
               }
           }
           catch
           {
               // Fallback: бинарная проверка если JSON не распарсился
               state.IsGrounded = response.Trim().ToLower().Contains("true") ||
                                  response.Trim().ToLower().Contains("yes");
               step.Metadata["fallback"] = true;
           }
       }
       catch (Exception ex)
       {
           step.Metadata["error"] = ex.Message;
           // При ошибке считаем ответ grounded (чтобы не зациклиться)
           state.IsGrounded = true;
       }
       finally
       {
           state.ExecutionSteps.Add(step);
       }
       
       return state;
   }

   public class HallucinationResult
   {
       public bool is_grounded { get; set; }
       public float confidence { get; set; }
       public List<string> hallucinated_parts { get; set; }
       public string explanation { get; set; }
   }

4. ДОБАВЬ УЗЕЛ Regenerate:
   private async Task<RagState> RegenerateNodeAsync(RagState state, CancellationToken ct)
   {
       var step = new ExecutionStep { NodeName = "Regenerate" };
       
       try
       {
           // Сохраняем предыдущий ответ в историю
           state.AnswerHistory.Add(state.Answer);
           state.RegenerationCount++;
           
           // Формируем более строгий промпт для регенерации
           var context = string.Join("\n\n", state.Documents
               .Where(d => d.IsRelevant)
               .Select(d => d.Text));
           
           var prompt = $@"
Ты - ассистент, который отвечает на вопросы СТРОГО по контексту.
Предыдущий ответ был признан содержащим выдумки (галлюцинации).

ВАЖНЕЙШИЕ ПРАВИЛА:
1. НЕ ИСПОЛЬЗУЙ СВОИ ЗНАНИЯ
2. Отвечай ТОЛЬКО информацией из контекста
3. Если информации нет в контексте - скажи "В контексте нет информации"
4. Каждое утверждение должно быть подтверждено контекстом
5. Лучше сказать "не знаю", чем придумать

Контекст:
{context}

Вопрос: {state.Question}

Ответ (строго по контексту, без выдумок):";
           
           var newAnswer = await _ollama.GenerateAsync(prompt, ct);
           state.Answer = newAnswer;
           
           step.Metadata["regeneration_count"] = state.RegenerationCount;
           step.Metadata["previous_answer_length"] = state.AnswerHistory.Last().Length;
       }
       catch (Exception ex)
       {
           step.Metadata["error"] = ex.Message;
       }
       finally
       {
           state.ExecutionSteps.Add(step);
       }
       
       return state;
   }

5. ОБНОВИ ЛОГИКУ ГРАФА:
   // После GenerateNode:
   state = await GenerateNodeAsync(state, ct);
   
   // Проверка на галлюцинации
   state = await HallucinationCheckNodeAsync(state, ct);
   
   // Если не grounded и есть попытки - регенерируем
   if (!state.IsGrounded && state.RegenerationCount < _config.Value.Hallucination.MaxRegenerations)
   {
       state = await RegenerateNodeAsync(state, ct);
       
       // Проверяем снова (опционально)
       state = await HallucinationCheckNodeAsync(state, ct);
   }
   
   // Если после регенерации все еще не grounded - добавляем предупреждение
   if (!state.IsGrounded)
   {
       state.Answer += "\n\n⚠️ **Предупреждение**: Ответ может содержать элементы, не найденные в документах.";
   }

6. ОБНОВИ AskQuestionTool:
   // В формировании ответа добавить:
   if (state.RegenerationCount > 0)
   {
       result.AppendLine($"🔄 **Регенераций:** {state.RegenerationCount}");
   }
   
   if (state.GroundingScore.HasValue)
   {
       result.AppendLine($"🎯 **Уверенность в фактах:** {state.GroundingScore:P1}");
   }
   
   if (state.AnswerHistory.Any())
   {
       result.AppendLine("📜 **История ответов:**");
       for (int i = 0; i < state.AnswerHistory.Count; i++)
       {
           result.AppendLine($"  {i + 1}. {state.AnswerHistory[i].Substring(0, Math.Min(100, state.AnswerHistory[i].Length))}...");
       }
   }