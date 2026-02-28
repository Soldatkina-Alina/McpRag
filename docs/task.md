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


Задача 4: Проверка связи с Ollama
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

Задача 5: Простой запрос к LLM
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

index_folder теперь читает содержимое файлов

Сохраняет текст в память (List<string>)

Инструмент list_files показывает загруженное

Пока без эмбеддингов

📝 Промт:

text
Улучши IndexFolderTool - добавь чтение содержимого файлов.

Требования:
1. Создай интерфейс IIndexerService с методом: Task<List<FileContent>> LoadFilesAsync(string folderPath, string pattern)
   где FileContent: { string Path, string Content, long Size }
2. Реализуй чтение всех текстовых файлов (.txt, .md, .cs, .json - все что можно прочесть как текст)
3. Содержимое сохраняй в памяти (просто список в поле класса)
4. Инструмент index_folder возвращает: "Загружено X файлов, общий размер Y символов"
5. Добавь тестовый инструмент list_files для просмотра загруженного (возвращает имена файлов)
6. Добавь очистку при новой индексации

Напиши код.
Проверь загрузку разных файлов.
🔍 Проверка:

bash
# Создать тестовые файлы
echo "Содержимое файла 1" > test_docs/file1.txt
echo "public class Test { }" > test_docs/file2.cs

# В Cline: "Проиндексируй папку ./test_docs"
# Должен вернуть: "Загружено 2 файлов, общий размер 50 символов"

# В Cline: "Покажи загруженные файлы"
# Должен вернуть: "file1.txt, file2.cs"
Задача 7: Эмбеддинги и векторный поиск в памяти
 Что должно работать:

Генерация эмбеддингов через Ollama

Хранение векторов в памяти

Поиск похожих документов по запросу

Инструмент search_docs для поиска

📝 Промт:

text
Добавь эмбеддинги и векторный поиск.

Требования:
1. Добавь в IOllamaService метод: Task<float[]> GenerateEmbeddingsAsync(string text, CancellationToken ct)
2. Создай интерфейс IVectorStoreService с методами:
   - Task AddDocumentsAsync(IEnumerable<DocumentChunk> chunks)
   - Task<IEnumerable<DocumentChunk>> SearchAsync(string query, int topK)
   - Task ClearAsync()
3. Создай класс DocumentChunk: { Id, Text, Source, Embedding, IndexedAt }
4. Реализуй ChromaDbService (in-memory):
   - Хранит чанки в List<DocumentChunk>
   - При поиске генерирует эмбеддинг запроса
   - Считает косинусное сходство
5. При index_folder теперь:
   - Разбивай файлы на чанки (просто по 1000 символов пока)
   - Генерируй эмбеддинги для каждого чанка
   - Сохраняй в векторное хранилище
6. Добавь инструмент search_docs(query, topK=5) для поиска

Напиши код.
Проверь поиск: найди документы по смыслу.
🔍 Проверка:

bash
# Проиндексировать папку с документами
# В Cline: "Найди документы по запросу 'программирование'"
# Должен вернуть список похожих документов с фрагментами
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