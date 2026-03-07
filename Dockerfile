# Stage 1: Build and publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy csproj and restore
COPY McpRag/*.csproj ./McpRag/
RUN dotnet restore McpRag/McpRag.csproj

# Copy source code
COPY McpRag/ ./McpRag/

# Publish
RUN dotnet publish McpRag/McpRag.csproj -c Release -o /app/publish /p:UseAppHost=true

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Create directory for logs
RUN mkdir -p logs

# Expose necessary ports
EXPOSE 5000 8080

# Set environment variables
ENV OLLAMA_HOST=http://ollama:11434
ENV CHROMADB_HOST=http://chromadb:8000
ENV DOTNET_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["./McpRag"]
