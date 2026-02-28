# Запросы использованные в работе над проектом McpRag

## Основные запросы к MCP серверу

### Запрос к инструменту echo
```json
{
  "server_name": "McpRag",
  "tool_name": "echo",
  "arguments": {
    "message": "Привет"
  }
}
```
**Описание**: Вызов инструмента echo для вывода сообщения "Привет"

### Запрос к инструменту index_folder
```json
{
  "server_name": "McpRag",
  "tool_name": "index_folder",
  "arguments": {
    "folderPath": "./test_docs",
    "pattern": "*.*"
  }
}
```
**Описание**: Вызов инструмента index_folder для подсчета всех файлов в папке test_docs

### Запрос к инструменту index_folder с паттерном
```json
{
  "server_name": "McpRag",
  "tool_name": "index_folder",
  "arguments": {
    "folderPath": "./test_docs",
    "pattern": "*.md"
  }
}
```
**Описание**: Вызов инструмента index_folder для подсчета Markdown файлов в папке test_docs

### Запрос к инструменту check_ollama
```json
{
  "server_name": "McpRag",
  "tool_name": "check_ollama",
  "arguments": {}
}
```
**Описание**: Вызов инструмента check_ollama для проверки доступности Ollama и наличия нужных моделей

## Запросы к Cline

### Запрос для вызова инструмента echo через Cline
```
Вызови инструмент echo с сообщением 'Привет'
```

### Запрос для вызова инструмента index_folder через Cline
```
Проиндексируй папку ./test_docs
```

### Запрос для вызова инструмента index_folder с паттерном через Cline
```
Проиндексируй папку ./test_docs с паттерном *.md
```

### Запрос для вызова инструмента check_ollama через Cline
```
Проверь доступность Ollama
```

## Запросы для проверки состояния Ollama

### Проверка доступности порта Ollama
```bash
Test-NetConnection -ComputerName localhost -Port 11434
```
**Описание**: Проверка доступности порта 11434, на котором работает Ollama

### Проверка списка моделей через API
```bash
curl http://localhost:11434/api/tags
```
**Описание**: Прямой запрос к API Ollama для получения списка доступных моделей

### Проверка наличия конкретной модели
```bash
curl http://localhost:11434/api/tags | Select-String -Pattern "nomic-embed-text"
```
**Описание**: Проверка наличия модели nomic-embed-text в списке доступных моделей

## Команды для управления Ollama

### Запуск Ollama
```bash
ollama serve
```
**Описание**: Запуск сервера Ollama вручную

### Проверка процесса McpRag
```bash
Get-Process -Name McpRag -ErrorAction SilentlyContinue
```
**Описание**: Проверка, запущен ли процесс McpRag (в данном случае для проверки состояния сервера)

### Запуск MCP Inspector
```bash
npx @modelcontextprotocol/inspector dotnet run --project McpRag
```
**Описание**: Запуск MCP Inspector для тестирования инструментов сервера McpRag