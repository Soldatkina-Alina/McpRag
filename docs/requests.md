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