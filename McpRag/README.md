# MCP Server

This README was created using the C# MCP server project template.
It demonstrates how you can easily create an MCP server using C# and publish it as a NuGet package.

### ВНИМАНИЕ
Модели очень слабые. Вопросы через ask_question должны быть максимально простыми и быть частью текста. ChromeDB находит идеально, а вот LLM урезает ответ и может выдать чушь или вовсе сказать, что ответ не найден.

## Запуск в Docker

Для запуска RAG сервера в Docker выполните следующие шаги:

### Требования
- Docker и Docker Compose
- Docker Desktop (для Windows/macOS)

### Быстрый запуск

1. **Склонируйте репозиторий:**
   ```bash
   git clone https://github.com/Soldatkina-Alina/McpRag.git
   cd McpRag
   ```

2. **Запустите контейнеры:**
   ```bash
   docker-compose up --build -d
   ```

3. **Проверьте состояние контейнеров:**
   ```bash
   docker-compose ps
   ```

### Что запускается

- **Ollama** (порт 11434): Сервис для работы с LLM моделями
- **ChromaDB** (порт 8000): Векторная база данных для хранения эмбеддингов
- **RAG Server** (порт 5000): Основной сервер приложения

### Проверка работоспособности

1. **Проверьте доступность сервисов:**
   ```bash
   # Проверка Ollama
   curl http://localhost:11434/api/tags
   
   # Проверка ChromaDB
   curl http://localhost:8000/api/v1/heartbeat
   
   # Проверка RAG сервера
   curl http://localhost:5000/health
   ```

2. **Проверка логов:**
   ```bash
   docker-compose logs rag-server
   ```

### Остановка сервисов

```bash
docker-compose down
```

### Полная очистка

```bash
docker-compose down -v
```
### Для подключения к Cline
{
  "mcpServers": {
    "McpRag": {
      "autoApprove": [
        "echo",
        "check_ollama",
        "list_files",
        "ask_llm",
        "index_folder",
        "vector_store_status",
        "clear_vector_store",
        "search_docs",
        "ask_question",
        "index_status",
        "find_relevant_docs",
        "summarize_document"
      ],
      "disabled": false,
      "timeout": 6000,
      "type": "stdio",
      "command": "docker",
      "args": [
        "exec",
        "-i",
        "server",
        "dotnet", "McpRag.dll"
      ]
    }
  }
}

### Особенности

- Модели Ollama автоматически загружаются при первом запуске
- Данные сохраняются в Docker volumes
- Сервер доступен по адресу `http://localhost:5000`
- Логи приложения сохраняются в volume `server_logs`

## Project Overview

The MCP server is built as a self-contained application and does not require the .NET runtime to be installed on the target machine.
However, since it is self-contained, it must be built for each target platform separately.
By default, the template is configured to build for:
* `win-x64`
* `win-arm64`
* `osx-arm64`
* `linux-x64`
* `linux-arm64`
* `linux-musl-x64`

If your users require more platforms to be supported, update the list of runtime identifiers in the project's `<RuntimeIdentifiers />` element.

See [aka.ms/nuget/mcp/guide](https://aka.ms/nuget/mcp/guide) for the full guide.

Please note that this template is currently in an early preview stage. If you have feedback, please take a [brief survey](http://aka.ms/dotnet-mcp-template-survey).

## Checklist before publishing to NuGet.org

- Test the MCP server locally using the steps below.
- Update the package metadata in the .csproj file, in particular the `<PackageId>`.
- Update `.mcp/server.json` to declare your MCP server's inputs.
  - See [configuring inputs](https://aka.ms/nuget/mcp/guide/configuring-inputs) for more details.
- Pack the project using `dotnet pack`.

The `bin/Release` directory will contain the package file (.nupkg), which can be [published to NuGet.org](https://learn.microsoft.com/nuget/nuget-org/publish-a-package).

## Developing locally

To test this MCP server from source code (locally) without using a built MCP server package, you can configure your IDE to run the project directly using `dotnet run`.

```json
{
  "servers": {
    "McpRag": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "<PATH TO PROJECT DIRECTORY>"
      ]
    }
  }
}
```

Refer to the VS Code or Visual Studio documentation for more information on configuring and using MCP servers:

- [Use MCP servers in VS Code (Preview)](https://code.visualstudio.com/docs/copilot/chat/mcp-servers)
- [Use MCP servers in Visual Studio (Preview)](https://learn.microsoft.com/visualstudio/ide/mcp-servers)

## Testing the MCP Server

Once configured, you can ask Copilot Chat for a random number, for example, `Give me 3 random numbers`. It should prompt you to use the `get_random_number` tool on the `McpRag` MCP server and show you the results.

## Publishing to NuGet.org

1. Run `dotnet pack -c Release` to create the NuGet package
2. Publish to NuGet.org with `dotnet nuget push bin/Release/*.nupkg --api-key <your-api-key> --source https://api.nuget.org/v3/index.json`

## Using the MCP Server from NuGet.org

Once the MCP server package is published to NuGet.org, you can configure it in your preferred IDE. Both VS Code and Visual Studio use the `dnx` command to download and install the MCP server package from NuGet.org.

- **VS Code**: Create a `<WORKSPACE DIRECTORY>/.vscode/mcp.json` file
- **Visual Studio**: Create a `<SOLUTION DIRECTORY>\.mcp.json` file

For both VS Code and Visual Studio, the configuration file uses the following server definition:

```json
{
  "servers": {
    "McpRag": {
      "type": "stdio",
      "command": "dnx",
      "args": [
        "<your package ID here>",
        "--version",
        "<your package version here>",
        "--yes"
      ]
    }
  }
}
```

## More information

.NET MCP servers use the [ModelContextProtocol](https://www.nuget.org/packages/ModelContextProtocol) C# SDK. For more information about MCP:

- [Official Documentation](https://modelcontextprotocol.io/)
- [Protocol Specification](https://spec.modelcontextprotocol.io/)
- [GitHub Organization](https://github.com/modelcontextprotocol)
- [MCP C# SDK](https://modelcontextprotocol.github.io/csharp-sdk)