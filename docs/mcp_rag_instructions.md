# Инструкция по использованию сервера McpRag

## Общая информация

Сервер McpRag - это локальный MCP-сервер, который предоставляет инструменты для работы с файлами и папками.

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
}
```

### 2. index_folder
**Назначение**: Подсчет файлов в папке по заданному шаблону
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

## Как обращаться к серверу

Для использования инструментов сервера McpRag необходимо:

1. **Настройка сервера в Cline**: Настройки сервера хранятся в файле `cline_mcp_settings.json` (в директории `docs/`). Этот файл содержит конфигурацию для подключения к локальному MCP-серверу.

2. **Использование инструмента `use_mcp_tool`**:
   - Укажите имя сервера: `McpRag`
   - Выберите нужный инструмент: `echo`, `index_folder` или `check_ollama`
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

## Текущее состояние

Сервер McpRag работает корректно и готов к использованию. Все инструменты протестированы и функционируют как ожидается.