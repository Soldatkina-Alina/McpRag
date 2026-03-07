# Task 14 - Docker Containerization

## Created Files

### 1. Dockerfile
- Multi-stage Dockerfile for building and running the MCP server
- Stage 1: Build stage using .NET SDK 10.0
- Stage 2: Publish stage to create a production-ready build
- Stage 3: Runtime stage using .NET Runtime 10.0
- Exposes ports 5000 and 8080
- Sets environment variables for Ollama and ChromaDB connection
- Creates log directory

### 2. docker-compose.yml
- Defines three services: ollama, chromadb, and rag-server
- Uses healthchecks to ensure services are ready before dependent services start
- Exposes ports 11434 (Ollama), 8000 (ChromaDB), and 5000/8080 (RAG server)
- Volume mounts for persistent data storage
- Uses a custom bridge network for communication between containers

### 3. init-ollama.sh
- Shell script to automatically download required Ollama models
- Waits for Ollama service to be available
- Checks available models using Ollama API
- Pulls phi3:mini (for generation) and nomic-embed-text (for embeddings)

## Instructions for Running

### Prerequisites
1. Docker Desktop installed and running
2. Windows Subsystem for Linux (WSL) 2 installed (for Docker on Windows)

### Step 1: Build and Start Services
```bash
# Build and start all services in detached mode
docker-compose up --build -d

# Check service status
docker-compose ps

# View logs (optional)
docker-compose logs -f
```

### Step 2: Verify Services are Running
```bash
# Check Ollama health
curl -f http://localhost:11434/api/tags

# Check ChromaDB health
curl -f http://localhost:8000/api/v1/heartbeat

# Check RAG server logs
docker-compose logs rag-server
```

### Step 3: Connect from Cline
1. Open VS Code
2. Open Cline extension
3. Go to MCP Servers
4. Add new server with command: `docker run -it --network mcprag-network --volume $PWD/test_docs:/app/test_docs mcprag-server:latest`
5. Connect to the server
6. Test the tools

### Step 4: Stopping Services
```bash
# Stop all services
docker-compose down

# Stop and remove volumes
docker-compose down -v
```

### Step 5: Cleanup (optional)
```bash
# Remove all containers
docker rm -f $(docker ps -a -q)

# Remove all volumes
docker volume rm $(docker volume ls -q)

# Remove images
docker rmi -f mcprag-server:latest chromadb/chroma:latest ollama/ollama:latest
```

## Configuration

### Environment Variables
- **OLLAMA_HOST**: http://ollama:11434 (internal Docker network)
- **CHROMADB_HOST**: http://chromadb:8000 (internal Docker network)
- **DOTNET_ENVIRONMENT**: Production

### Volumes
- **ollama_data**: Persistent storage for Ollama models
- **chroma_data**: Persistent storage for ChromaDB documents
- **server_logs**: Storage for server log files
- **./test_docs**: Mounted test document directory

## Healthchecks
- **Ollama**: Checks /api/tags endpoint (30s interval, 60s start period)
- **ChromaDB**: Checks /api/v1/heartbeat endpoint (30s interval, 60s start period)
- **RAG Server**: Runs --check command (30s interval, 60s start period)

## Notes

1. First run may take time as Ollama models are downloaded (approx 2GB)
2. Test documents should be placed in the `test_docs` directory
3. Services can take up to 2 minutes to fully initialize
4. Ensure Docker has enough resources (CPU, memory) allocated for running the models