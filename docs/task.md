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

# 2. В Cline перезапусти сервер (Restart в панели MCP)

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


Задача 7: Эмбеддинги и векторный поиск в памяти
 
Задача 7: Эмбеддинги и векторный поиск в памяти

✅ Что должно работать:
- Генерация эмбеддингов через Ollama (модель nomic-embed-text)
- Разбивка документов на чанки с перекрытием
- Хранение векторов в памяти с метаданными
- Поиск похожих документов по запросу (косинусное сходство)
- Инструмент search_docs для поиска
- Параллельная обработка для скорости

📝 Промт:

Добавь эмбеддинги и векторный поиск.

⚠️ ВАЖНО: Для эмбеддингов используется модель nomic-embed-text
Убедись, что она установлена: ollama pull nomic-embed-text

Требования:

1. Добавь в IOllamaService метод:
   Task<float[]> GenerateEmbeddingsAsync(string text, CancellationToken ct = default)

2. Создай класс DocumentChunk:
   public class DocumentChunk
   {
       public string Id { get; set; } = Guid.NewGuid().ToString();
       public string Text { get; set; }
       public string Source { get; set; }  // путь к файлу
       public int ChunkIndex { get; set; }
       public float[] Embedding { get; set; }
       public DateTime IndexedAt { get; set; } = DateTime.UtcNow;
       public Dictionary<string, object> Metadata { get; set; } = new();
   }

3. Создай интерфейс IVectorStoreService с методами:
   - Task AddDocumentsAsync(IEnumerable<DocumentChunk> chunks, CancellationToken ct)
   - Task<IEnumerable<DocumentChunk>> SearchAsync(string query, int topK = 5, CancellationToken ct)
   - Task ClearAsync(CancellationToken ct)
   - Task<int> CountAsync(CancellationToken ct)

4. Реализуй VectorStoreService (in-memory):
   - Хранит чанки в List<DocumentChunk> (потокобезопасно)
   - При добавлении нормализуй эмбеддинги (делай единичную длину)
   - При поиске:
     * Генерируй эмбеддинг запроса
     * Нормализуй его
     * Считай косинусное сходство со всеми чанками
     * Возвращай topK наиболее похожих
   - Добавь лок для потокобезопасности

5. Создай TextSplitter:
   public class TextSplitter
   {
       public List<string> Split(string text, int chunkSize, int chunkOverlap)
       {
           // Реализуй рекурсивную разбивку:
           // 1. Сначала по параграфам (\n\n)
           // 2. Потом по предложениям (.!?)
           // 3. Потом по словам (пробел)
       }
   }

6. Обнови IndexerService:
   - Принимай ITextSplitter и IVectorStoreService
   - При индексации:
     * Для каждого файла загружай содержимое (из Задачи 6)
     * Разбивай на чанки через TextSplitter
     * Для каждого чанка генерируй эмбеддинги (параллельно, но с ограничением)
     * Создавай DocumentChunk с метаданными
     * Сохраняй в векторное хранилище
   - Добавь настройки в IndexerConfig:
     * ChunkSize (по умолчанию 1000)
     * ChunkOverlap (по умолчанию 200)
     * MaxConcurrency (по умолчанию 5)

7. Добавь инструмент search_docs:
   - Принимает query (string) и topK (int, default 5)
   - Проверяет, есть ли документы в индексе
   - Вызывает IVectorStoreService.SearchAsync
   - Возвращает форматированный список:
     * Релевантность в процентах
     * Имя файла и номер чанка
     * Фрагмент текста (первые 200 символов)

8. Добавь в конфигурацию:
   {
     "VectorStore": {
       "ChunkSize": 1000,
       "ChunkOverlap": 200,
       "MaxConcurrency": 5,
       "SimilarityThreshold": 0.7
     }
   }

Напиши полный код:
- DocumentChunk.cs
- IVectorStoreService.cs
- VectorStoreService.cs
- ITextSplitter.cs
- TextSplitter.cs
- Обновленный IndexerService.cs
- SearchDocsTool.cs
- Обновленный IndexerConfig.cs
- Обновленный Program.cs с регистрацией

🔍 Проверки (через Cline):

# 1. Подготовь тестовые файлы с разной тематикой
echo @"
C# - это объектно-ориентированный язык программирования, 
разработанный компанией Microsoft для платформы .NET.
Он сочетает мощь C++ с простотой Visual Basic.
"@ > test_docs/csharp.txt

echo @"
Python - это интерпретируемый язык программирования 
с динамической типизацией. Широко используется в 
машинном обучении, анализе данных и веб-разработке.
"@ > test_docs/python.txt

echo @"
JavaScript - это язык программирования, который работает 
в браузерах и на сервере (Node.js). Основной язык 
для фронтенд-разработки.
"@ > test_docs/javascript.txt

# 2. Перезапусти сервер в Cline

# 3. Проиндексируй
"Проиндексируй папку ./test_docs"

# 4. Тест 1: Поиск по языку Microsoft (должен найти C#)
"Найди документы по запросу 'язык от Microsoft для .NET'"

# Ожидаемый результат: csharp.txt с высокой релевантностью (>80%)

# 5. Тест 2: Поиск по машинному обучению (должен найти Python)
"Найди документы по запросу 'машинное обучение и анализ данных'"

# Ожидаемый результат: python.txt с высокой релевантностью

# 6. Тест 3: Поиск по браузеру (должен найти JavaScript)
"Найди документы по запросу 'язык для браузеров'"

# Ожидаемый результат: javascript.txt с высокой релевантностью

# 7. Тест 4: Проверка количества результатов
"Найди 3 документа по запросу 'язык программирования'"

# Ожидаемый результат: все три файла, отсортированные по релевантности

# 8. Тест 5: Проверка на пустом индексе
"Очисти индекс" (если добавил)
"Найди документы по запросу 'тест'"
# Ожидаемый результат: "Векторное хранилище пусто. Сначала выполните index_folder"


Задача 8: Простейший RAG (поиск + генерация)
 Что должно работать:

Инструмент ask_question ищет документы и отправляет в LLM

Возвращает ответ с источниками

Пока без графа состояний

📝 Промт:

text
Реализуй простейший RAG: поиск + генерация.

Требования:
1. Добавь инструмент ask_question(question)
2. Логика:
   - Ищет 3 наиболее релевантных чанка через векторное хранилище
   - Формирует промпт: "Контекст: {текст чанков}\n\nВопрос: {question}\n\nОтвет:"
   - Отправляет в LLM через OllamaService.GenerateAsync
   - Возвращает ответ
3. Добавь в ответ список источников (имена файлов)
4. Обработай случай, если документов нет

Напиши код.
Проверь на реальных вопросах по документам.
🔍 Проверка:

bash
# Проиндексировать документацию по C#
# В Cline: "Спроси: что такое async/await?"
# Должен ответить на основе загруженных документов
# В ответе должны быть источники: "file1.cs, file2.md"
Задача 9: Базовая структура графа состояний
 Что должно работать:

Граф с двумя узлами: Search и Generate

Состояние передается между узлами

В ответе виден путь выполнения

📝 Промт:

text
Добавь простую версию графа состояний.

Требования:
1. Создай класс RagState:
   - string Question
   - string Answer
   - List<DocumentChunk> Documents
   - List<string> ExecutionPath
2. Создай интерфейс IRagGraphService с методом Task<RagState> ExecuteAsync(string question)
3. Создай RagGraphService:
   - Узел SearchNode: вызывает векторный поиск, сохраняет в state.Documents
   - Узел GenerateNode: формирует промпт, вызывает LLM, сохраняет в state.Answer
   - Каждый узел добавляет свое имя в ExecutionPath
4. Инструмент ask_question использует граф
5. Добавь в ответ путь выполнения

Напиши код.
Проверь, что в ответе видно: Search → Generate.
🔍 Проверка:

bash
# В Cline: "Спроси: ..."
# В ответе должно быть: "Путь выполнения: Search → Generate"
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