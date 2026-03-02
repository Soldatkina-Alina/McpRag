СПИСОК ЗАДАЧ ДЛЯ РАЗРАБОТКИ MCP СЕРВЕРА

✅ Задача 1: Добавление тестового инструмента Echo
 Что должно работать:

Сервер имеет инструмент "echo"

Инструмент принимает message и возвращает "Echo: " + message

Можно подключиться через MCP Inspector и вызвать инструмент

📝 Промт:

text
Добавь в MCP сервер один тестовый инструмент EchoTool.

Требования:
1. Удалить демо-инструмент GetRandomNumber (оставить только пустую регистрацию)
2. Добавить минимальное логирование "Server started" при запуске
3. Инструмент должен называться "echo" и принимать параметр "message" (string)
4. Просто возвращает: "Echo: " + message
5. Зарегистрировать инструмент через DI
6. Добавить минимальное логирование вызовов

Напиши код инструмента и обновленный Program.cs.

После этого кратко допиши в result_task.md папки dosc, что было сделано, а также инструкцию:
- Как установить MCP Inspector (npx @modelcontextprotocol/inspector)
- Как подключиться к серверу
- Как вызвать инструмент echo и проверить ответ
🔍 Проверка:

bash
# Терминал 1
dotnet run

# Терминал 2
npx @modelcontextprotocol/inspector dotnet run
# В браузере http://localhost:5173 - должен быть виден инструмент echo


✅ Задача 2: Подключение к Cline
 Что должно работать:

Сервер виден в Cline

Инструмент echo доступен из чата Cline

Можно вызвать echo и получить ответ

📝 Промт:

text
Настрой подключение MCP сервера к Cline.

Требования:
1. Создай файл .vscode/mcp.json с конфигурацией для VS Code

Создай файл в папке dosc
1. Напиши содержимое для cline_mcp_settings.json
2. Дай инструкцию: как добавить сервер в Cline


Напиши содержимое обоих конфигурационных файлов и подробную инструкцию.

🔍 Проверка ручная:

Открыть VS Code

Открыть Cline (иконка робота)

Нажать "MCP Servers" → Проверь, что инструмент echo появляется в списке MCP серверов Cline -> должен быть виден сервер

В чате написать: "Вызови инструмент echo с сообщением 'Привет'"

Должен ответить: "Echo: Привет"

✅ Задача 3: Инструмент index_folder (подсчет файлов)
 Что должно работать:

Инструмент index_folder принимает путь к папке и паттерн

Возвращает количество найденных файлов

Проверяет существование папки

Пока без реальной индексации и LLM

📝 Промт:

text
Добавь инструмент IndexFolderTool (первая версия, без LLM и эмбеддингов).

Требования:
1. Инструмент принимает folderPath (string) и pattern (string, по умолчанию "*.*")
2. Просто подсчитывает количество файлов по паттерну
3. Возвращает строку: "Найдено X файлов в папке Y"
4. Проверяет, существует ли папка, иначе возвращает ошибку
5. Добавить логирование вызовов

Напиши код инструмента.
Индексация и LLM НЕ нужны на этом этапе - только подсчет файлов.

Проверь работу через MCP Inspector и Cline.
🔍 Проверка:

bash
# Создать тестовую папку
mkdir test_docs
echo "test" > test_docs/file1.txt
echo "test" > test_docs/file2.txt

# В Cline: "Проиндексируй папку ./test_docs"
# Должен вернуть: "Найдено 2 файлов в папке ./test_docs"

# В Cline: "Проиндексируй папку ./test_docs с паттерном *.md"
# Должен вернуть: "Найдено 0 файлов в папке ./test_docs"


✅Задача 4: Проверка связи с Ollama
 Что должно работать:

Добавь в проект проверку связи с Ollama.

ВАЖНО: Для корректной работы тебе понадобятся две разные модели Ollama:
- Для эмбеддингов (векторный поиск): nomic-embed-text
- Для генерации ответов: phi3:mini (или qwen2.5:7b для лучшего качества)

Проверь, что они установлены. Если нет, то установи их перед началом:
ollama pull nomic-embed-text
ollama pull phi3:mini

Требования:

1. Создай интерфейс IOllamaService с методами:
   - Task<bool> IsHealthyAsync(CancellationToken ct = default)
   - Task<List<string>> ListModelsAsync(CancellationToken ct = default)
   - Task<bool> IsModelAvailableAsync(string modelName, CancellationToken ct = default)

2. Создай класс конфигурации OllamaConfig:
   public class OllamaConfig
   {
       public string BaseUrl { get; set; } = "http://localhost:11434";
       public string Model { get; set; } = "phi3:mini";
       public string EmbeddingModel { get; set; } = "nomic-embed-text";
       public int TimeoutSeconds { get; set; } = 30;
   }

3. Реализуй OllamaService, который:
   - Проверяет эндпоинт /api/tags для проверки доступности
   - Получает список доступных моделей через /api/tags
   - Проверяет наличие конкретной модели по имени
   - Использует HttpClient с таймаутом
   - Логирует все запросы и ошибки

4. Зарегистрируй сервис в DI правильно:
   // Добавь конфигурацию
   builder.Services.Configure<OllamaConfig>(builder.Configuration.GetSection("Ollama"));
   
   // Зарегистрируй HttpClient с настройками
   builder.Services.AddHttpClient<IOllamaService, OllamaService>((sp, client) =>
   {
       var config = sp.GetRequiredService<IOptions<OllamaConfig>>().Value;
       client.BaseAddress = new Uri(config.BaseUrl);
       client.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
   });

5. Добавь инструмент check_ollama, который:
   - Проверяет доступность Ollama
   - Проверяет наличие обеих моделей (Model и EmbeddingModel из конфига)
   - Возвращает подробный статус:
     * "✅ Ollama доступна по адресу {url}"
     * "📋 Доступные модели: {список}"
     * "✅ Нужные модели установлены: phi3:mini, nomic-embed-text"
     * ИЛИ "⚠️ Отсутствуют модели: {список_отсутствующих}. Установи: ollama pull ..."

6. Добавь обработку ошибок:
   - Таймаут - возвращать понятное сообщение
   - Connection refused - сообщить, что Ollama не запущена
   - Любые исключения логировать и возвращать дружелюбное сообщение

7. Добавь в appsettings.json:
   {
     "Ollama": {
       "BaseUrl": "http://localhost:11434",
       "Model": "phi3:mini",
       "EmbeddingModel": "nomic-embed-text",
       "TimeoutSeconds": 30
     }
   }

Напиши полный код:
- IOllamaService.cs
- OllamaConfig.cs
- OllamaService.cs
- CheckOllamaTool.cs
- Фрагмент Program.cs с регистрацией
- Обновленный appsettings.json

🔍 Проверка:

# Запустить Ollama
ollama serve

# Установить модели (если еще не)
ollama pull phi3:mini
ollama pull nomic-embed-text

# В Cline: "Проверь доступность Ollama"
# Должен вернуть:
# ✅ Ollama доступна по адресу http://localhost:11434
# 📋 Доступные модели: phi3:mini, nomic-embed-text, ...
# ✅ Нужные модели установлены: phi3:mini, nomic-embed-text

# Остановить Ollama (Ctrl+C)
# В Cline: "Проверь доступность Ollama"
# Должен вернуть: "❌ Ollama не доступна. Убедитесь, что она запущена"

✅Задача 5: Простой запрос к LLM
 Что должно работать:

Добавь простой запрос к LLM без RAG.

ВАЖНО: Для генерации ответов используется модель, указанная в конфигурации (по умолчанию phi3:mini).
Модель должна быть установлена заранее: ollama pull phi3:mini

Требования:

1. Добавь в IOllamaService метод:
   Task<string> GenerateAsync(string prompt, CancellationToken ct = default)

2. Создай модели для ответа Ollama:
   public class OllamaGenerateResponse
   {
       [JsonPropertyName("model")]
       public string Model { get; set; }
       
       [JsonPropertyName("response")]
       public string Response { get; set; }
       
       [JsonPropertyName("done")]
       public bool Done { get; set; }
       
       [JsonPropertyName("error")]
       public string Error { get; set; }
   }

3. Реализуй метод GenerateAsync:
   - Используй эндпоинт POST /api/generate
   - Отправляй JSON с полями:
     {
       "model": config.Model,
       "prompt": prompt,
       "stream": false,
       "options": {
         "temperature": 0.7,
         "num_predict": 500
       }
     }
   - Парси ответ и возвращай поле response
   - Если пришел error - выбрасывай исключение

4. Добавь в OllamaConfig новые поля (опционально):
   public double Temperature { get; set; } = 0.7;
   public int MaxTokens { get; set; } = 500;

5. Добавь инструмент ask_llm, который:
   - Принимает параметр question (string)
   - Проверяет доступность Ollama через IsHealthyAsync
   - Проверяет наличие модели через IsModelAvailableAsync
   - Отправляет вопрос в LLM
   - Возвращает ответ

6. Обработка ошибок:
   - Если Ollama не доступна: "❌ Ollama не доступна. Запустите ollama serve"
   - Если модель не найдена: "❌ Модель {model} не найдена. Установите: ollama pull {model}"
   - Если таймаут: "⏱️ Превышено время ожидания ответа от Ollama"
   - Любые другие ошибки логировать и возвращать понятное сообщение

7. Добавь поддержку отмены (CancellationToken):
   - Передавай токен в HttpClient запросы
   - При отмене выбрасывай OperationCanceledException

8. Обнови регистрацию в DI (если нужно):
   // Уже должно быть из Задачи 4

Напиши код:
- Обновленный IOllamaService.cs
- Обновленный OllamaService.cs с методом GenerateAsync
- OllamaGenerateResponse.cs
- AskLlmTool.cs
- Обновленный OllamaConfig.cs (с новыми полями)

🔍 Проверка:

# В Cline тест 1: простой вопрос
# Запрос: "Спроси у LLM: что такое RAG?"
# Ожидаемый результат: связный ответ о RAG (Retrieval-Augmented Generation)

# В Cline тест 2: вопрос на русском
# Запрос: "Спроси у LLM: как работает семантический поиск?"
# Ожидаемый результат: ответ на русском языке

Задача 6: Чтение содержимого файлов
 Что должно работать:
- index_folder теперь читает содержимое файлов
- Сохраняет текст в память (List<FileContent>)
- Инструмент list_files показывает загруженное
- Пока без эмбеддингов
- Поддержка разных кодировок
- Фильтрация по расширениям из конфига

📝 Промт:

Улучши IndexFolderTool - добавь чтение содержимого файлов.

Требования:

1. Создай интерфейс IIndexerService с методом:
   Task<List<FileContent>> LoadFilesAsync(string folderPath, string pattern, CancellationToken ct)

2. Создай класс FileContent:
   public class FileContent
   {
       public string Path { get; set; }
       public string FileName { get; set; }
       public string Extension { get; set; }
       public string Content { get; set; }
       public long Size { get; set; }
       public DateTime LastModified { get; set; }
   }

3. Добавь конфигурацию IndexerConfig:
   public class IndexerConfig
   {
       public List<string> SupportedExtensions { get; set; } = 
           new List<string> { ".txt", ".md", ".cs", ".js", ".ts", ".json", ".yaml", ".rst" };
       public int MaxFileSizeMB { get; set; } = 10;
       public bool SkipLockedFiles { get; set; } = true;
   }

4. Реализуй IndexerService:
   - Рекурсивно обходит все файлы по паттерну
   - Фильтрует по расширениям из конфига
   - Проверяет размер файла (не больше MaxFileSizeMB)
   - Читает содержимое в UTF-8 кодировке
   - Обрабатывает исключения (FileNotFoundException, UnauthorizedAccessException)
   - Сохраняет метаданные (размер, дата изменения)
   - Хранит список в памяти (с очисткой при новой индексации)

5. Добавь инструмент index_folder:
   - Принимает folderPath и pattern
   - Вызывает LoadFilesAsync
   - Возвращает: "Загружено {count} файлов, общий размер {totalSize} символов"
   - Если файлы пропущены (слишком большие/заблокированные) - сообщить об этом

6. Добавь инструмент list_files:
   - Принимает опциональный параметр extension для фильтрации
   - Возвращает список файлов с их размером и датой изменения
   - Формат: "file1.txt (1.2 KB, 2024-01-01), file2.cs (3.5 KB, 2024-01-02)"

7. Добавь очистку при новой индексации:
   - При вызове index_folder старые данные удаляются
   - list_files показывает только текущие загруженные файлы

8. Добавь обработку ошибок:
   - Если папка не существует: "Ошибка: папка {folderPath} не найдена"
   - Если нет файлов по паттерну: "Не найдено файлов, соответствующих паттерну {pattern}"
   - Если файлы пропущены: "Загружено {count} файлов. Пропущено {skipped} (слишком большие/заблокированные)"

Напиши полный код:
- IIndexerService.cs
- FileContent.cs
- IndexerConfig.cs
- IndexerService.cs
- IndexFolderTool.cs (обновленный)
- ListFilesTool.cs (новый)
- Обновленный Program.cs с регистрацией
- Обновленный appsettings.json

Задача 6.1.
🔍 Проверки (через Cline, НЕ через терминал):

# 1. Создай тестовые файлы (в терминале, не в Cline)
mkdir test_docs
echo "Содержимое файла 1" > test_docs/file1.txt
echo "public class Test { }" > test_docs/file2.cs
echo "{\"name\":\"test\"}" > test_docs/config.json
echo "Очень большой текст" > test_docs/large.txt

# 3. Протестируй index_folder (можно на русском)
"Проиндексируй папку ./test_docs"

# Ожидаемый ответ:
# "Загружено 4 файлов, общий размер 150 символов"

# 4. Проверь список файлов
"Покажи загруженные файлы"

# Ожидаемый ответ:
# "file1.txt (1.2 KB, 2024-01-01), file2.cs (0.5 KB, 2024-01-01), config.json (0.3 KB, 2024-01-01), large.txt (2.1 KB, 2024-01-01)"

# 5. Проверь фильтрацию по расширению
"Покажи только .txt файлы"

# 6. Проверь повторную индексацию (должна очистить старые данные)
"Проиндексируй папку ./test_docs с паттерном *.txt"
"Покажи загруженные файлы"  # должны быть только .txt файлы

# 7. Проверь обработку ошибок
"Проиндексируй папку ./несуществующая_папка"
# Ожидаемый ответ: "Ошибка: папка ./несуществующая_папка не найдена"


✅ Задача 7: Эмбеддинги и векторный поиск в памяти
 
Добавь интеграцию с настоящей векторной БД ChromaDB.

Перед началом:

1. Запусти ChromaDB в контейнере:
   docker run -d -p 8000:8000 --name chromadb chromadb/chroma

2. Проверь, что работает:
   curl http://localhost:8000/api/v1/heartbeat

Требования:

1. Создай интерфейс IVectorStoreService (остается как был):
   - Task AddDocumentsAsync(IEnumerable<DocumentChunk> chunks, CancellationToken ct)
   - Task<IEnumerable<DocumentChunk>> SearchAsync(string query, int topK, CancellationToken ct)
   - Task ClearAsync(CancellationToken ct)
   - Task<int> CountAsync(CancellationToken ct)

2. Создай класс DocumentChunk (остается как был):
   public class DocumentChunk
   {
       public string Id { get; set; } = Guid.NewGuid().ToString();
       public string Text { get; set; }
       public string Source { get; set; }
       public int ChunkIndex { get; set; }
       public float[] Embedding { get; set; }
       public DateTime IndexedAt { get; set; } = DateTime.UtcNow;
       public Dictionary<string, object> Metadata { get; set; } = new();
   }

3. Установи NuGet пакет для ChromaDB:
   dotnet add package ChromaDB.Client

4. Реализуй ChromaDbService, который использует настоящую ChromaDB:
   public class ChromaDbService : IVectorStoreService
   {
       private readonly ChromaCollection _collection;
       private readonly IOllamaService _ollama;
       
       public ChromaDbService(IOllamaService ollama)
       {
           _ollama = ollama;
           
           // Подключение к ChromaDB в Docker
           var client = new ChromaClient(new HttpClient
           {
               BaseAddress = new Uri("http://localhost:8000")
           });
           
           // Создаем или получаем коллекцию "documents"
           _collection = client.GetOrCreateCollectionAsync("documents").Result;
       }
       
       public async Task AddDocumentsAsync(IEnumerable<DocumentChunk> chunks, CancellationToken ct)
       {
           var chunksList = chunks.ToList();
           
           // Подготовка данных для ChromaDB
           var ids = chunksList.Select(c => c.Id).ToList();
           var embeddings = chunksList.Select(c => c.Embedding).ToList();
           var documents = chunksList.Select(c => c.Text).ToList();
           
           // Метаданные - важны для фильтрации и отображения
           var metadatas = chunksList.Select(c => new Dictionary<string, object>
           {
               ["source"] = c.Source,
               ["chunk_index"] = c.ChunkIndex,
               ["indexed_at"] = c.IndexedAt.ToString("o"),
               ["file_name"] = Path.GetFileName(c.Source),
               ["extension"] = Path.GetExtension(c.Source)
           }).ToList();
           
           // Добавление в ChromaDB
           await _collection.AddAsync(
               ids: ids,
               embeddings: embeddings,
               metadatas: metadatas,
               documents: documents
           );
       }
       
       public async Task<IEnumerable<DocumentChunk>> SearchAsync(string query, int topK, CancellationToken ct)
       {
           // Генерируем эмбеддинг запроса
           var queryEmbedding = await _ollama.GenerateEmbeddingsAsync(query, ct);
           
           // Поиск в ChromaDB
           var results = await _collection.QueryAsync(
               queryEmbeddings: new[] { queryEmbedding },
               nResults: topK
           );
           
           // Преобразование результатов обратно в DocumentChunk
           return results.Select(r => new DocumentChunk
           {
               Id = r.Id,
               Text = r.Document,
               Source = r.Metadata?["source"]?.ToString(),
               ChunkIndex = int.Parse(r.Metadata?["chunk_index"]?.ToString() ?? "0"),
               IndexedAt = DateTime.Parse(r.Metadata?["indexed_at"]?.ToString() ?? DateTime.UtcNow.ToString()),
               Metadata = r.Metadata?.ToDictionary(x => x.Key, x => x.Value) ?? new()
           }).ToList();
       }
       
       public async Task ClearAsync(CancellationToken ct)
       {
           // Удаляем все документы из коллекции
           await _collection.DeleteAsync(new Dictionary<string, object>());
       }
       
       public async Task<int> CountAsync(CancellationToken ct)
       {
           return await _collection.CountAsync();
       }
   }

5. Обнови IndexerService для работы с настоящим векторным хранилищем:
   - При индексации разбивай на чанки (TextSplitter из Задачи 6)
   - Генерируй эмбеддинги через Ollama
   - Создавай DocumentChunk
   - Сохраняй через ChromaDbService

6. Добавь инструмент search_docs:
   - Принимает query (string) и topK (int, default 5)
   - Проверяет, есть ли документы (CountAsync)
   - Вызывает SearchAsync
   - Возвращает форматированный результат с:
     * Имя файла, номер чанка
     * Фрагмент текста
     * Метаданные

7. Добавь инструмент vector_store_status:
   - Показывает статистику ChromaDB
   - Количество документов
   - Размер коллекции
   - Адрес сервера

8. Добавь конфигурацию в appsettings.json:
   {
     "VectorStore": {
       "Type": "chromadb",  // chromadb или in-memory
       "ConnectionString": "http://localhost:8000",
       "CollectionName": "documents",
       "ChunkSize": 1000,
       "ChunkOverlap": 200
     }
   }

Напиши полный код:
- ChromaDbService.cs
- Обновленный IndexerService.cs
- SearchDocsTool.cs
- VectorStoreStatusTool.cs
- Обновленный Program.cs с регистрацией
- Обновленный appsettings.json

Задача 7.1 Проверка:


# 1. Запусти ChromaDB
docker run -d -p 8000:8000 --name chromadb chromadb/chroma

# 2. Проверь, что ChromaDB работает
curl http://localhost:8000/api/v1/heartbeat

# 3. Создай тестовые файлы
mkdir test_docs
echo @"
C# - язык программирования от Microsoft для .NET платформы.
Поддерживает объектно-ориентированное, функциональное и асинхронное программирование.
"@ > test_docs/csharp.txt

echo @"
Python - интерпретируемый язык с динамической типизацией.
Популярен в машинном обучении, анализе данных и веб-разработке.
"@ > test_docs/python.txt

# 4. Остановись на этом пункте. Далее только ручная проверка

# 5. Проиндексируй документы
"Проиндексируй папку ./test_docs"

# 6. Проверь статус векторного хранилища
"Покажи статус векторного хранилища"
# Ожидаемый ответ: "ChromaDB: 2 документа, коллекция 'documents', сервер localhost:8000"

# 7. Найди документы по смыслу
"Найди документы по запросу 'язык для машинного обучения'"
# Ожидаемый ответ: первым должен быть python.txt

"Найди документы по запросу 'платформа Microsoft'"
# Ожидаемый ответ: первым должен быть csharp.txt

# 8. Проверь, что данные сохранились
# Останови MCP сервер, перезапусти, и снова выполни поиск
# Данные должны быть доступны (не пропали)

# 9. Очисти хранилище (если нужно)
"Очисти векторное хранилище"
"Покажи статус векторного хранилища"
# Ожидаемый ответ: 0 документов


Задача 8: Простейший RAG (поиск + генерация)

Задача 8: Простейший RAG (поиск + генерация)

Что должно работать:
- Инструмент ask_question ищет документы через ChromaDB
- Отправляет контекст в LLM с правильным форматированием
- Возвращает ответ с явными ссылками на источники
- Соблюдает порог релевантности
- Обрабатывает слишком длинный контекст
- Понятно сообщает, если информации нет

📝 Промт:

Реализуй простейший RAG: поиск + генерация.

Требования:

1. Добавь в конфигурацию настройки:
   {
     "RAG": {
       "MaxChunks": 5,
       "MinRelevanceScore": 0.7,
       "MaxContextTokens": 2000,
       "IncludeMetadataInContext": true
     }
   }

2. Создай класс для форматирования контекста:
   public class ContextFormatter
   {
       public string FormatContext(List<DocumentChunk> chunks, bool includeMetadata = true)
       {
           var sb = new StringBuilder();
           for (int i = 0; i < chunks.Count; i++)
           {
               var chunk = chunks[i];
               sb.AppendLine($"--- [Источник {i+1}]: {Path.GetFileName(chunk.Source)} ---");
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

3. Добавь инструмент ask_question:
   [McpServerTool]
   public async Task<string> AskQuestion(
       [Description("Вопрос к базе знаний")] string question,
       [Description("Минимальная релевантность (0-1)")] double? minScore = null,
       [Description("Максимальное количество чанков")] int? maxChunks = null,
       CancellationToken cancellationToken = default)
   {
       try
       {
           // 1. Получаем настройки
           var actualMinScore = minScore ?? _config.MinRelevanceScore;
           var actualMaxChunks = maxChunks ?? _config.MaxChunks;
           
           // 2. Поиск релевантных чанков
           var chunks = await _vectorStore.SearchAsync(question, actualMaxChunks, cancellationToken);
           var chunksList = chunks.ToList();
           
           // 3. Проверка порога релевантности
           var relevantChunks = chunksList
               .Where(c => c.Score >= actualMinScore)
               .ToList();
           
           if (!relevantChunks.Any())
           {
               return "❌ В предоставленных документах не найдено информации по вашему вопросу.\n\n" +
                      $"Наиболее похожие документы имеют релевантность {chunksList.FirstOrDefault()?.Score:P1}, " +
                      $"что ниже порога {actualMinScore:P1}.";
           }
           
           // 4. Форматирование контекста
           var context = _contextFormatter.FormatContext(relevantChunks);
           
           // 5. Формирование промпта для LLM
           var prompt = $@"
Ты - ассистент, который отвечает на вопросы, используя ТОЛЬКО информацию из предоставленного контекста.

ВАЖНЫЕ ПРАВИЛА:
1. Отвечай строго на основе контекста, не используй свои знания
2. Если в контексте нет ответа - скажи, что информации нет
3. Ссылайся на источники в формате [Источник N]
4. Не придумывай факты и не дополняй информацию
5. Если информация неполная - так и скажи

Контекст (документы):
{context}

Вопрос пользователя: {question}

Ответ (только на основе контекста, с указанием источников):";
           
           // 6. Отправка в LLM
           var answer = await _ollama.GenerateAsync(prompt, cancellationToken);
           
           // 7. Формирование финального ответа с источниками
           var sources = relevantChunks
               .Select(c => Path.GetFileName(c.Source))
               .Distinct()
               .ToList();
           
           var result = new StringBuilder();
           result.AppendLine(answer);
           result.AppendLine();
           result.AppendLine("---");
           result.AppendLine("📚 **Источники:**");
           foreach (var source in sources)
           {
               result.AppendLine($"- {source}");
           }
           result.AppendLine();
           result.AppendLine($"*Найдено {relevantChunks.Count} релевантных чанков " +
                            $"из {chunksList.Count} проверенных*");
           
           return result.ToString();
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "Ошибка при обработке вопроса");
           return $"❌ Ошибка при обработке вопроса: {ex.Message}";
       }
   }

4. Добавь обработку слишком длинного контекста:
   - В методе SearchAsync передавай лимит токенов
   - Если контекст превышает лимит - обрезай до наиболее релевантных чанков
   - Добавь предупреждение в ответ

5. Обнови IVectorStoreService.SearchAsync, чтобы возвращал Score:
   public class SearchResult
   {
       public DocumentChunk Chunk { get; set; }
       public float Score { get; set; }
   }
   
   Task<List<SearchResult>> SearchWithScoreAsync(string query, int topK, CancellationToken ct);

6. Добавь в ChromaDbService метод SearchWithScoreAsync:
   - Возвращает чанки вместе с их релевантностью (distance преобразуй в score)

Напиши полный код:
- RAGConfig.cs
- ContextFormatter.cs
- AskQuestionTool.cs (обновленный)
- SearchResult.cs
- Обновленный IVectorStoreService.cs
- Обновленный ChromaDbService.cs с SearchWithScoreAsync
- Обновленный appsettings.json

Задача 8.1 Проверка (через Cline):

# 1. Подготовь тестовые документы
echo @"
C# поддерживает асинхронное программирование через async/await.
Ключевое слово async указывает, что метод содержит await.
await приостанавливает выполнение до завершения ожидаемой задачи.
"@ > test_docs/async.txt

echo @"
LINQ (Language Integrated Query) позволяет писать декларативные запросы к коллекциям.
Пример: var result = collection.Where(x => x > 5).Select(x => x * 2);
LINQ работает с IEnumerable и IQueryable.
"@ > test_docs/linq.txt

echo @"
Рекурсия - это вызов функцией самой себя.
Важно иметь базовый случай для завершения рекурсии.
Пример: factorial(n) = n * factorial(n-1) с base case n <= 1.
"@ > test_docs/recursion.txt

# 2. Проиндексируй
"Проиндексируй папку ./test_docs"

# 3. Тест 1: Прямой вопрос (должен найти async.txt)
"Спроси: объясни как работает async/await в C#"
# Ожидаемый ответ: объяснение из async.txt с ссылкой [Источник 1]

# 4. Тест 2: Вопрос без точного совпадения (должен найти LINQ)
"Спроси: как фильтровать коллекции в C#"
# Ожидаемый ответ: про LINQ с ссылкой на linq.txt

# 5. Тест 3: Вопрос без ответа
"Спроси: что такое dependency injection"
# Ожидаемый ответ: сообщение о том, что информации нет

# 6. Тест 4: Проверка порога релевантности
"Спроси: расскажи про рекурсию с минимальной релевантностью 0.9"
# Ожидаемый ответ: если релевантность < 0.9, должно сказать "нет информации"

# 7. Тест 5: Множественные источники
"Спроси: расскажи про основные возможности C#"
# Ожидаемый ответ: информация из разных файлов с разными источниками

Задача 9: Базовая структура графа состояний
Добавь простую версию графа состояний, постепенно заменяя прямой RAG из Задачи 8.

Требования:

1. СОЗДАЙ ОБЩИЙ КОНФИГ (переиспользуй из Задачи 8):
   public class RAGConfig
   {
       public int MaxChunks { get; set; } = 5;
       public float MinRelevanceScore { get; set; } = 0.7f;
       public int MaxContextTokens { get; set; } = 2000;
       public bool IncludeMetadataInContext { get; set; } = true;
   }

2. ОБНОВИ DocumentChunk (добавь поле Score):
   public class DocumentChunk
   {
       // существующие поля из Задачи 7
       public string Id { get; set; }
       public string Text { get; set; }
       public string Source { get; set; }
       public int ChunkIndex { get; set; }
       public float[] Embedding { get; set; }
       public DateTime IndexedAt { get; set; }
       public Dictionary<string, object> Metadata { get; set; } = new();
       
       // НОВОЕ: поле для релевантности от ChromaDB
       public float Score { get; set; }
   }

3. СОЗДАЙ КЛАССЫ ДЛЯ СОСТОЯНИЯ:
   public class ExecutionStep
   {
       public string NodeName { get; set; }
       public DateTime Timestamp { get; set; } = DateTime.UtcNow;
       public Dictionary<string, object> Metadata { get; set; } = new();
   }

   public class RagState
   {
       public string Question { get; set; }
       public List<DocumentChunk> Documents { get; set; } = new();
       public string Answer { get; set; }
       public List<ExecutionStep> ExecutionSteps { get; set; } = new();
       public bool HasError { get; set; }
       public string ErrorMessage { get; set; }
       
       // Вспомогательные свойства
       public bool HasRelevantDocuments => 
           Documents.Any(d => d.Score >= _config.MinRelevanceScore);
   }

4. СОЗДАЙ ИНТЕРФЕЙС IRagGraphService:
   public interface IRagGraphService
   {
       Task<RagState> ExecuteAsync(string question, CancellationToken ct = default);
   }

5. РЕАЛИЗУЙ RagGraphService (ПЕРЕИСПОЛЬЗУЯ код из Задачи 8):
   public class RagGraphService : IRagGraphService
   {
       private readonly IVectorStoreService _vectorStore;
       private readonly IOllamaService _ollama;
       private readonly IOptions<RAGConfig> _config;
       private readonly ContextFormatter _contextFormatter;
       private readonly ILogger<RagGraphService> _logger;

       public RagGraphService(
           IVectorStoreService vectorStore,
           IOllamaService ollama,
           IOptions<RAGConfig> config,
           ContextFormatter contextFormatter,
           ILogger<RagGraphService> logger)
       {
           _vectorStore = vectorStore;
           _ollama = ollama;
           _config = config;
           _contextFormatter = contextFormatter;
           _logger = logger;
       }

       public async Task<RagState> ExecuteAsync(string question, CancellationToken ct)
       {
           var state = new RagState { Question = question };
           
           try
           {
               // Узел 1: Search (с сохранением Score)
               state = await SearchNodeAsync(state, ct);
               
               // Проверка релевантности (как в Задаче 8)
               if (!state.HasRelevantDocuments)
               {
                   var maxScore = state.Documents.FirstOrDefault()?.Score ?? 0;
                   state.Answer = $"❌ В предоставленных документах не найдено информации.\n" +
                                  $"Наиболее похожие документы имеют релевантность {maxScore:P1}, " +
                                  $"что ниже порога {_config.Value.MinRelevanceScore:P1}.";
                   
                   state.ExecutionSteps.Add(new ExecutionStep 
                   { 
                       NodeName = "NoRelevantDocs",
                       Metadata = new Dictionary<string, object>
                       {
                           ["max_score"] = maxScore,
                           ["threshold"] = _config.Value.MinRelevanceScore
                       }
                   });
                   return state;
               }
               
               // Узел 2: Generate (используя ContextFormatter из Задачи 8)
               state = await GenerateNodeAsync(state, ct);
               
               return state;
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error executing RAG graph");
               state.HasError = true;
               state.ErrorMessage = ex.Message;
               state.ExecutionSteps.Add(new ExecutionStep 
               { 
                   NodeName = "Error",
                   Metadata = new Dictionary<string, object> { ["error"] = ex.Message }
               });
               return state;
           }
       }

       private async Task<RagState> SearchNodeAsync(RagState state, CancellationToken ct)
       {
           var step = new ExecutionStep { NodeName = "Search" };
           
           try
           {
               // Используем SearchWithScoreAsync для получения релевантности
               var results = await _vectorStore.SearchWithScoreAsync(
                   state.Question, 
                   _config.Value.MaxChunks, 
                   ct);
               
               // Сохраняем документы вместе с их Score
               state.Documents = results.Select(r => 
               {
                   r.Chunk.Score = r.Score;
                   return r.Chunk;
               }).ToList();
               
               step.Metadata["chunks_found"] = state.Documents.Count;
               step.Metadata["max_score"] = state.Documents.FirstOrDefault()?.Score ?? 0;
               step.Metadata["threshold"] = _config.Value.MinRelevanceScore;
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

       private async Task<RagState> GenerateNodeAsync(RagState state, CancellationToken ct)
       {
           var step = new ExecutionStep { NodeName = "Generate" };
           
           try
           {
               // Фильтруем только релевантные документы (как в Задаче 8)
               var relevantChunks = state.Documents
                   .Where(d => d.Score >= _config.Value.MinRelevanceScore)
                   .ToList();
               
               // Используем ContextFormatter из Задачи 8
               var context = _contextFormatter.FormatContext(
                   relevantChunks, 
                   _config.Value.IncludeMetadataInContext);
               
               // Проверка длины контекста
               if (context.Length > _config.Value.MaxContextTokens * 4) // грубая оценка
               {
                   context = context.Substring(0, _config.Value.MaxContextTokens * 4) + 
                            "\n\n...[контекст обрезан]";
                   step.Metadata["truncated"] = true;
               }
               
               // Промпт из Задачи 8 (проверенный, надежный)
               var prompt = $@"
Ты - ассистент, который отвечает на вопросы, используя ТОЛЬКО информацию из предоставленного контекста.

ВАЖНЫЕ ПРАВИЛА:
1. Отвечай строго на основе контекста, не используй свои знания
2. Если в контексте нет ответа - скажи, что информации нет
3. Ссылайся на источники в формате [Источник N]
4. Не придумывай факты и не дополняй информацию
5. Если информация неполная - так и скажи

Контекст (документы):
{context}

Вопрос пользователя: {state.Question}

Ответ (только на основе контекста, с указанием источников):";
               
               state.Answer = await _ollama.GenerateAsync(prompt, ct);
               
               step.Metadata["chunks_used"] = relevantChunks.Count;
               step.Metadata["context_length"] = context.Length;
               step.Metadata["truncated"] = context.Length > _config.Value.MaxContextTokens * 4;
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
   }

6. ЗАРЕГИСТРИРУЙ СЕРВИСЫ В DI:
   builder.Services.AddScoped<IRagGraphService, RagGraphService>();
   builder.Services.AddSingleton<ContextFormatter>(); // переиспользуем из Задачи 8

7. ОБНОВИ AskQuestionTool для использования графа:
   [McpServerTool]
   public async Task<string> AskQuestion(
       [Description("Вопрос к базе знаний")] string question,
       CancellationToken cancellationToken)
   {
       try
       {
           var state = await _ragGraph.ExecuteAsync(question, cancellationToken);
           
           if (state.HasError)
           {
               return $"❌ Ошибка: {state.ErrorMessage}";
           }
           
           var result = new StringBuilder();
           result.AppendLine(state.Answer);
           result.AppendLine();
           result.AppendLine("---");
           result.AppendLine("📊 **Путь выполнения:**");
           
           foreach (var step in state.ExecutionSteps)
           {
               result.AppendLine($"- **{step.NodeName}** ({step.Timestamp:HH:mm:ss})");
               if (step.Metadata.Any())
               {
                   var meta = string.Join(", ", step.Metadata.Select(m => $"{m.Key}: {m.Value}"));
                   result.AppendLine($"  *{meta}*");
               }
           }
           
           // Добавляем источники (как в Задаче 8)
           if (state.Documents.Any())
           {
               var sources = state.Documents
                   .Where(d => d.Score >= _config.Value.MinRelevanceScore)
                   .Select(d => Path.GetFileName(d.Source))
                   .Distinct();
               
               result.AppendLine();
               result.AppendLine("📚 **Источники:**");
               foreach (var source in sources)
               {
                   result.AppendLine($"- {source}");
               }
               
               result.AppendLine();
               result.AppendLine($"*Найдено {state.Documents.Count(d => d.Score >= _config.Value.MinRelevanceScore)} " +
                                $"релевантных чанков из {state.Documents.Count} проверенных*");
           }
           
           return result.ToString();
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "Error in AskQuestion");
           return $"❌ Ошибка: {ex.Message}";
       }
   }

8. ДОБАВЬ КОНФИГУРАЦИЮ В appsettings.json:
   {
     "RAG": {
       "MaxChunks": 5,
       "MinRelevanceScore": 0.7,
       "MaxContextTokens": 2000,
       "IncludeMetadataInContext": true
     }
   }

Напиши полный код:
- Обновленный DocumentChunk.cs (с полем Score)
- ExecutionStep.cs
- RagState.cs
- IRagGraphService.cs
- RagGraphService.cs
- Обновленный AskQuestionTool.cs
- Фрагмент Program.cs с регистрацией
- Обновленный appsettings.json

Задача 10: Оценка релевантности документов
 Что должно работать:

Узел GradeDocuments оценивает релевантность

Неподходящие документы отбрасываются

Если нет релевантных - возвращается "не знаю"

📝 Промт:

text
Добавь проверку релевантности документов.

Требования:
1. Добавь в граф узел GradeDocuments
2. Логика узла:
   - Для каждого документа в state.Documents
   - Промпт: "Оцени, релевантен ли этот документ для вопроса. Ответь только 'yes' или 'no'.\n\nВопрос: {question}\n\nДокумент: {text}"
   - Если LLM ответила 'yes' - оставляем документ
3. Обнови ExecutionPath
4. Если после оценки нет документов - узел Generate возвращает "Не могу найти ответ в документах"
5. Добавь в ответ количество релевантных документов

Напиши код.
Проверь на вопросах, которых нет в документах.
🔍 Проверка:

bash
# Задать вопрос, которого нет в документах
# Должен вернуть: "Не могу найти ответ в документах"
# Путь выполнения: Search → Grade → NoDocs
Задача 11: Retry-цикл с переписыванием запроса
 Что должно работать:

Узел RewriteQuery улучшает запрос

При недостатке документов запрос расширяется

Максимум 2 попытки

📝 Промт:

text
Добавь retry-цикл в граф.

Требования:
1. Добавь в RagState: string RewrittenQuery, int RetryCount
2. Добавь узел RewriteQuery:
   - Промпт: "Улучши поисковый запрос для RAG системы. Исходный запрос: {question}. Напиши только улучшенную версию."
   - Сохраняет в state.RewrittenQuery
3. Добавь узел BroadenQuery:
   - Промпт: "Расширь поисковый запрос, чтобы найти больше информации. Текущий запрос: {query}. Напиши только расширенную версию."
4. Логика:
   - После GradeDocuments проверяем: если релевантных < 2 и RetryCount < 2
   - Тогда: RetryCount++ → BroadenQuery → Search (с новым запросом) → GradeDocuments
5. Обнови ExecutionPath

Напиши код.
Проверь, что при пустом результате делает до 2 попыток.
🔍 Проверка:

bash
# Задать сложный вопрос
# В ответе должно быть видно: Rewrite → Search → Grade → (мало) → Broaden → Search → Grade → Generate
Задача 12: Проверка на галлюцинации
   Что должно работать:

Узел HallucinationCheck проверяет ответ

При галлюцинациях ответ регенерируется

Максимум одна регенерация

📝 Промт:

text
Добавь проверку на галлюцинации.

Требования:
1. Добавь в RagState: bool IsGrounded
2. Добавь узел HallucinationCheck:
   - Промпт: "Проверь, основан ли ответ на предоставленном контексте. Ответь только 'yes' если ответ полностью основан на контексте, или 'no' если есть выдумки.\n\nКонтекст: {documents}\n\nОтвет: {answer}\n\nРезультат:"
   - Устанавливает state.IsGrounded = (ответ 'yes')
3. Добавь узел Regenerate:
   - Промпт: "Ответь на вопрос строго по контексту. НЕ ДОБАВЛЯЙ ИНФОРМАЦИЮ ОТ СЕБЯ.\n\nКонтекст: {documents}\n\nВопрос: {question}\n\nОтвет:"
   - Генерирует новый ответ
4. Логика:
   - Generate → HallucinationCheck
   - Если не grounded → Regenerate (один раз)
5. Обнови ExecutionPath

Напиши код.
Проверь на вопросах, где LLM может добавить свое.
🔍 Проверка:

bash
# Задать вопрос, на который LLM может "фантазировать"
# В пути должно быть: Generate → Check → (не grounded) → Regenerate
Задача 13: Все 5 инструментов
 Что должно работать:

index_folder (полная версия с индексацией)

ask_question (полный граф)

find_relevant_docs (поиск без генерации)

summarize_document (саммари файла)

index_status (статистика)

📝 Промт:

text
Добавь оставшиеся три инструмента для завершения набора из 5.

Требования:

1. find_relevant_docs:
   - Принимает query (string) и topK (int, default 5)
   - Использует IVectorStoreService.SearchAsync
   - Возвращает список чанков с источниками и фрагментами текста

2. summarize_document:
   - Принимает filePath (string)
   - Загружает файл через IIndexerService
   - Промпт: "Создай краткое содержание документа. Выдели основные темы.\n\nДокумент: {text}\n\nКраткое содержание:"
   - Возвращает саммари + информацию о размере

3. index_status:
   - Использует IVectorStoreService.GetStatisticsAsync()
   - Возвращает: общее количество чанков, количество файлов, дату последней индексации

4. Все инструменты должны быть зарегистрированы и иметь Description

Напиши код.
Проверь каждый инструмент отдельно.
🔍 Проверка:

bash
# Проверить find_relevant_docs
# Проверить summarize_document для файла
# Проверить index_status после индексации
Задача 14: Docker контейнеризация
 Что должно работать:

Dockerfile собирает образ

docker-compose поднимает сервер + Ollama

Сервер доступен из Cline в контейнере

📝 Промт:

text
Создай Docker конфигурацию для проекта.

Требования:
1. Dockerfile с многоступенчатой сборкой:
   - build stage: mcr.microsoft.com/dotnet/sdk:10.0
   - runtime stage: mcr.microsoft.com/dotnet/runtime:10.0
2. docker-compose.yml с двумя сервисами:
   - ollama: image ollama/ollama:latest, порт 11434, volume для данных
   - rag-server: build ., depends_on: ollama, environment: OLLAMA_HOST=http://ollama:11434
3. Добавь healthcheck для ollama
4. Добавь скрипт init-ollama.sh для загрузки моделей при старте
5. Напиши инструкцию по запуску

Напиши все файлы.
Проверь, что сервер работает в Docker и доступен из Cline.
🔍 Проверка:

bash
docker-compose up --build

# В другом терминале
# Подключиться к серверу из Cline (изменить command в настройках)
# Проверить все инструменты
Задача 15: Тесты
 Что должно работать:

Минимум 5 тестов проходят

Тесты используют моки

Можно запустить одной командой

📝 Промт:

text
Добавь минимальные тесты для проекта.

Требования (минимум 5 тестов):

1. Тест индексатора:
   - Создать временный файл
   - Вызвать LoadAndSplitDocumentAsync
   - Проверить, что вернулся хотя бы 1 чанк

2. Тест векторного поиска:
   - Добавить тестовый чанк в хранилище
   - Выполнить поиск
   - Проверить, что чанк найден

3. Тест графа с моком Ollama:
   - Замокать IOllamaService
   - Вызвать граф
   - Проверить ExecutionPath

4. Тест инструмента echo:
   - Создать инструмент
   - Вызвать с тестовым сообщением
   - Проверить ответ

5. Тест обработки ошибок:
   - Передать несуществующий путь в index_folder
   - Проверить, что возвращается сообщение об ошибке

Используй xUnit и Moq.
Напиши код тестов.
🔍 Проверка:

bash
dotnet test
# Все тесты должны быть зелеными