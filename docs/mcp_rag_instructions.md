# Инструкция по использованию сервера McpRag

## Общая информация

Сервер McpRag - это локальный MCP-сервер, который предоставляет инструменты для работы с файлами и папками, а также для векторного поиска и анализа документов с использованием LLM (Large Language Model).

## Доступные инструменты

### 1. echo
**Назначение**: Вывод сообщений
**Параметры**:
- `message` (string) - сообщение для вывода

**Пример использования**:
```json
{
  "server_name": "McpRag",
  "tool_name": "echo", 
  "arguments": {
    "message": "Привет"
}
```

### 2. index_folder
**Назначение**: Подсчет файлов в папке по заданному шаблону и индексация их содержимого для последующего поиска
**Параметры**:
- `folderPath` (string) - путь к папке
- `pattern` (string, optional) - шаблон поиска файлов (по умолчанию "*.txt")

**Пример использования**:
```json
{
  "server_name": "McpRag",
  "tool_name": "index_folder",
  "arguments": {
    "folderPath": "c:\\Users\\alink\\source\\repos\\McpRag\\test_docs",
    "pattern": "*.txt"
  }
}
```

### 3. ask_llm
**Назначение**: Прямой запрос к LLM (Large Language Model)
**Параметры**:
- `question` (string) - вопрос для отправки в LLM

**Пример использования**:
```json
{
  "server_name": "McpRag",
  "tool_name": "ask_llm",
  "arguments": {
    "question": "What is RAG?"
  }
}
```

### 4. list_files
**Назначение**: Вывод списка загруженных файлов с метаданными
**Параметры**:
- `extension` (string, optional) - фильтр по расширению файла (например, ".txt", "cs")

**Пример использования**:
```json
{
  "server_name": "McpRag",
  "tool_name": "list_files",
  "arguments": {
    "extension": ".txt"
  }
}
```

### 5. search_docs
**Назначение**: Поиск релевантных документов в векторном хранилище по текстовому запросу
**Параметры**:
- `query` (string) - строка запроса
- `topK` (int, optional) - количество возвращаемых результатов (по умолчанию 5)

**Пример использования**:
```json
{
  "server_name": "McpRag",
  "tool_name": "search_docs",
  "arguments": {
    "query": "C# programming",
    "topK": 3
  }
}
```

### 6. vector_store_status
**Назначение**: Проверка статуса векторного хранилища и его очистка
**Методы**:
1. `VectorStoreStatus` - возвращает информацию о состоянии vector store
2. `ClearVectorStore` - очищает все документы из vector store

**Пример использования**:
```json
{
  "server_name": "McpRag",
  "tool_name": "vector_store_status",
  "arguments": {}
}
```

### 7. check_ollama
**Назначение**: Проверка доступности Ollama и наличия нужных моделей
**Параметры**: Нет (инструмент не требует параметров)

**Пример использования**:
```json
{
  "server_name": "McpRag",
  "tool_name": "check_ollama",
  "arguments": {}
}
```

## Как обращаться к серверу

Для использования инструментов сервера McpRag необходимо:

1. **Настройка сервера в Cline**: Настройки сервера хранятся в файле `cline_mcp_settings.json` (в директории самой cline). Этот файл содержит конфигурацию для подключения к локальному MCP-серверу.

2. **Использование инструмента `use_mcp_tool`**:
   - Укажите имя сервера: `McpRag`
   - Выберите нужный инструмент: `echo`, `index_folder`, `check_ollama`, `ask_llm`, `list_files`, `search_docs` или `vector_store_status`
   - Передайте соответствующие параметры в `arguments`

## Примеры успешного использования

### Вызов echo
```json
{
  "server_name": "McpRag",
  "tool_name": "echo",
  "arguments": {
    "message": "Привет"
  }
}
```
**Результат**: "Echo: Привет"

### Индексация папки
```json
{
  "server_name": "McpRag", 
  "tool_name": "index_folder",
  "arguments": {
    "folderPath": "c:\\Users\\alink\\source\\repos\\McpRag\\test_docs",
    "pattern": "*.txt"
  }
}
```
**Результат**: "Найдено 2 файлов в папке c:\\Users\\alink\\source\\repos\\McpRag\\test_docs"

### Поиск документов
```json
{
  "server_name": "McpRag",
  "tool_name": "search_docs",
  "arguments": {
    "query": "C# programming",
    "topK": 3
  }
}
```
**Результат**: 
```
✅ Found 2 relevant documents:
1. [test_docs/csharp_basics.txt] C# is a modern, object-oriented programming language...
2. [test_docs/dotnet_framework.txt] The .NET Framework is a software framework...
```

### Проверка статуса vector store
```json
{
  "server_name": "McpRag",
  "tool_name": "vector_store_status",
  "arguments": {}
}
```
**Результат**:
```
✅ Vector store status:
- Type: chromadb
- Server: http://localhost:8000
- Collection: documents
- Documents: 12
```

## Проверка работы векторного поиска

### Предварительные условия
- **Docker установлен и запущен**
- **ChromaDB контейнер запущен**: `docker run -d -p 8000:8000 --name chromadb chromadb/chroma`
- **MCP сервер McpRag запущен** (работает в фоновом режиме)
- **Ollama запущена** с необходимыми моделями: phi3:mini и nomic-embed-text
- **Cline расширение установлено** в VS Code
- **Конфигурация в cline_mcp_settings.json** уже настроена и сервер McpRag подключен

### Пошаговая проверка

#### Шаг 1: Проверка подключения к ChromaDB
1. Откройте терминал и запустите ChromaDB:
   ```bash
   docker run -d -p 8000:8000 --name chromadb chromadb/chroma
   ```
2. Проверьте подключение:
   ```bash
   curl http://localhost:8000/api/v1/collections
   ```

#### Шаг 2: Проверка Ollama
1. В чате Cline введите запрос: **"Проверь доступность Ollama"**
2. Убедитесь, что все нужные модели доступны

#### Шаг 3: Индексация документов
1. Создайте тестовую папку:
   ```bash
   mkdir test_docs
   echo "C# is a modern, object-oriented programming language..." > test_docs/csharp_basics.txt
   echo "The .NET Framework is a software framework..." > test_docs/dotnet_framework.txt
   ```
2. В чате Cline индексируйте папку: **"Вызови инструмент index_folder с folderPath \"./test_docs\" и pattern \"*.txt\""**

#### Шаг 4: Поиск документов
1. В чате Cline выполните поиск: **"Вызови инструмент search_docs с query \"C# programming\" и topK 2"**
2. Проверьте результат

### Очистка векторного хранилища
Если нужно очистить все данные из vector store:
```json
{
  "server_name": "McpRag",
  "tool_name": "clear_vector_store",
  "arguments": {}
}
```

## Проверка работы check_ollama

### Предварительные условия
- **MCP сервер McpRag уже запущен** (работает в фоновом режиме)
- **Cline расширение установлено** в VS Code
- **Конфигурация в cline_mcp_settings.json** уже настроена и сервер McpRag подключен

### Пошаговая проверка

#### Шаг 1: Откройте Cline
1. В VS Code нажмите на иконку робота в панели расширений
2. Дождитесь загрузки интерфейса Cline

#### Шаг 2: Проверьте подключение сервера
1. В интерфейсе Cline найдите раздел **MCP Servers**
2. Убедитесь, что сервер **McpRag** отображается в списке и имеет статус "Connected"

#### Шаг 3: Выполните проверку Ollama
1. В чате Cline введите запрос: **"Проверь доступность Ollama"**
2. Или: **"Вызови инструмент check_ollama"**

#### Шаг 4: Проанализируйте результат
Сравните полученный ответ с ожидаемыми вариантами ниже.

**Возможные результаты**:

1. **Ollama запущена, все модели доступны**:
```
✅ Ollama доступна по адресу http://localhost:11434
📋 Доступные модели: phi3:mini, nomic-embed-text:latest, qwen2.5:7b, Codellama:latest, codeqwen:latest, DeepSeek-Coder:latest
✅ Нужные модели установлены: phi3:mini, nomic-embed-text
```

2. **Ollama не запущена**:
```
❌ Ollama не доступна. Убедитесь, что она запущена (ollama serve)
```

3. **Ollama запущена, но модели отсутствуют**:
```
⚠️ Отсутствуют модели: phi3:mini, nomic-embed-text. Установи: ollama pull phi3:mini && ollama pull nomic-embed-text
```

4. **Ollama запущена, часть моделей доступна**:
```
✅ Ollama доступна по адресу http://localhost:11434
📋 Доступные модели: phi3:mini, qwen2.5:7b, Codellama:latest
⚠️ Отсутствует модель: nomic-embed-text. Установи: ollama pull nomic-embed-text
```

### Альтернативные способы вызова
Если прямой текстовый запрос не работает:
- **"Используй инструмент check_ollama из сервера McpRag"**
- **"Выполни действие: вызов инструмента check_ollama"**

### Что делать при проблемах
- **Сервер не подключен**: Перезапустите Cline, проверьте конфигурацию
- **Инструмент не найден**: Убедитесь, что сервер McpRag запущен
- **Нет ответа**: Проверьте, что Ollama запущена, проверьте логи сервера

## Текущее состояние

Сервер McpRag работает корректно и готов к использованию. Все инструменты протестированы и функционируют как ожидается. Векторный поиск через ChromaDB и Ollama доступен для использования с любым текстовым запросом.