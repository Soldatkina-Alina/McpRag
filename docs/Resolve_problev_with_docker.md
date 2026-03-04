🛠 Пошаговое исправление
1. Исправь init-ollama
Проблема: В образе ollama/ollama нет curl. Нужно либо установить curl, либо использовать другой образ.

Решение А (проще): Использовать образ с curl

yaml
init-ollama:
  image: curlimages/curl:latest  # В этом образе есть curl
  container_name: mcprag-init-ollama
  depends_on:  # 👈 ВАЖНО: добавить зависимость!
    ollama:
      condition: service_healthy
  volumes:
    - ./init-ollama.sh:/init-ollama.sh:ro
  command: ["sh", "/init-ollama.sh"]  # 👈 sh, не bash (в curl образе sh)
  networks:
    - mcprag-network
  restart: "no"
Решение Б (более надежное): Использовать alpine и установить curl

yaml
init-ollama:
  image: alpine:latest
  container_name: mcprag-init-ollama
  depends_on:
    ollama:
      condition: service_healthy
  volumes:
    - ./init-ollama.sh:/init-ollama.sh:ro
  command: sh -c "apk add --no-cache curl && sh /init-ollama.sh"
  networks:
    - mcprag-network
  restart: "no"
2. Исправь healthcheck rag-server
Проблема: STDIO сервер не имеет HTTP endpoint для healthcheck.

Решение: Убери healthcheck или добавь HTTP endpoint в код.

Вариант А: Временно убрать healthcheck

yaml
rag-server:
  build: .
  container_name: mcprag-server
  ports:
    - "5000:5000"
    - "8080:8080"
  environment:
    - OLLAMA_HOST=http://ollama:11434
    - CHROMADB_HOST=http://chromadb:8000
    - DOTNET_ENVIRONMENT=Production
  volumes:
    - server_logs:/app/logs
    - ./test_docs:/app/test_docs
  depends_on:
    - ollama
    - chromadb
  # healthcheck: ... 👈 Временно закомментировать
  restart: unless-stopped
  networks:
    - mcprag-network
Вариант Б: Добавить HTTP endpoint в код (лучше, но сложнее)

3. Исправь переменные окружения
В Program.cs добавь маппинг переменных окружения в конфигурацию:

csharp
// Добавь это после builder.Services.Configure<OllamaConfig>
builder.Services.Configure<OllamaConfig>(options =>
{
    options.BaseUrl = Environment.GetEnvironmentVariable("OLLAMA_HOST") ?? "http://localhost:11434";
    // другие настройки...
});

builder.Services.Configure<VectorStoreConfig>(options =>
{
    options.BaseUrl = Environment.GetEnvironmentVariable("CHROMADB_HOST") ?? "http://localhost:8000";
    // другие настройки...
});
4. Исправь зависимости в docker-compose
У rag-server должна быть зависимость от init-ollama:

yaml
rag-server:
  # ...
  depends_on:
    - ollama
    - chromadb
    - init-ollama  # 👈 Добавить
5. Проверь инициализацию в Program.cs
В Program.cs есть проверка сервисов, но она может завершать приложение при неудаче:

csharp
if (!ollamaAvailable)
{
    logger.LogError(...);
    return;  // 👈 Это завершает приложение!
}
Это может вызывать перезапуск. Лучше сделать так:

csharp
if (!ollamaAvailable)
{
    logger.LogWarning("Ollama not available, continuing in degraded mode...");
    // Не завершаем, продолжаем работу
}
