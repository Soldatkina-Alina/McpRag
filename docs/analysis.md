# Проект McpRag - Анализ

## Обзор проекта

Проект McpRag представляет собой простой сервер, реализующий протокол Model Context Protocol (MCP) на платформе .NET 10.0. Сервер предоставляет инструменты для взаимодействия с AI-ассистентами, такими как GitHub Copilot Chat, через стандартные потоки ввода/вывода (stdio).

## Структура проекта

```
McpRag/
├── McpRag.csproj          # Файл конфигурации проекта
├── Program.cs             # Точка входа приложения
├── README.md              # Документация проекта
└── Tools/
    └── RandomNumberTools.cs  # Реализация инструментов для работы с случайными числами
```

## Основные технологии и зависимости

### Target Framework
- **.NET 10.0**

### NuGet Packages
1. **Microsoft.Extensions.Hosting (8.0.1)** - Предоставляет инфраструктуру для создания приложений с архитектурой Host.
2. **ModelContextProtocol (0.7.0-preview.1)** - SDK для реализации MCP-серверов на C#.

### Конфигурация проекта
- **SelfContained**: true - Приложение является самодостаточным и не требует установки .NET Runtime на целевой машине.
- **PublishSingleFile**: true - Приложение публикуется как один исполняемый файл.
- **RuntimeIdentifiers**: Поддерживаемые платформы: win-x64, win-arm64, osx-arm64, linux-x64, linux-arm64, linux-musl-x64.

## Реализация

### Точка входа - Program.cs

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Add the MCP services: the transport to use (stdio) and the tools to register.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<RandomNumberTools>();

await builder.Build().RunAsync();
```

В этом файле конфигурируется:
1. Логирование (все логи выводятся в stderr, чтобы не мешать протоколу MCP на stdout)
2. Сервер MCP с транспортным уровнем stdio
3. Регистрация инструментов для работы с сервером

### Инструменты - RandomNumberTools.cs

```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;

internal class RandomNumberTools
{
    [McpServerTool]
    [Description("Generates a random number between the specified minimum and maximum values.")]
    public int GetRandomNumber(
        [Description("Minimum value (inclusive)")] int min = 0,
        [Description("Maximum value (exclusive)")] int max = 100)
    {
        return Random.Shared.Next(min, max);
    }
}
```

Реализует один инструмент:
- `GetRandomNumber`: Генерирует случайное число в диапазоне [min, max), с значениями по умолчанию [0, 100).

## Назначение и использование

### Как работает MCP

MCP (Model Context Protocol) - это протокол взаимодействия между AI-ассистентами и внешними инструментами, разработанный для расширения возможностей чат-интерфейсов. При помощи MCP, AI может вызывать внешние инструменты, передавать им параметры и получать результаты.

### Сценарии использования

1. Локальная разработка и тестирование
2. Публикация как NuGet-пакет для широкого распространения
3. Интеграция с IDE: VS Code и Visual Studio

### Пример использования из IDE

После конфигурации сервер может быть вызван напрямую из GitHub Copilot Chat с запросом вида:
"Give me 3 random numbers"

Копилот предложит использовать инструмент `get_random_number` с сервером McpRag и покажет результаты.

## Процесс публикации на NuGet.org

1. Обновление метаданных в McpRag.csproj (PackageId, PackageVersion, Description)
2. Обновление .mcp/server.json для объявления входных параметров
3. Пакетирование проекта: `dotnet pack -c Release`
4. Публикация на NuGet.org: `dotnet nuget push bin/Release/*.nupkg --api-key <your-api-key> --source https://api.nuget.org/v3/index.json`

## Конфигурация в IDE

### VS Code
Создать файл `<WORKSPACE DIRECTORY>/.vscode/mcp.json`:

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

### Visual Studio
Создать файл `<SOLUTION DIRECTORY>\.mcp.json` с аналогичной структурой.

## Заключение

Проект McpRag демонстрирует базовую реализацию MCP-сервера на C# с использованием официального SDK. Это простой, но функциональный пример, показывающий, как создать инструменты для взаимодействия с AI-ассистентами через протокол MCP. Сервер поддерживает множество платформ и может быть опубликован как NuGet-пакет для широкого использования.