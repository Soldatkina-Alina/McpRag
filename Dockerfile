# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY McpRag/*.csproj ./McpRag/
COPY McpRag.Tests/*.csproj ./McpRag.Tests/
COPY McpRag.IntegrationTests/*.csproj ./McpRag.IntegrationTests/
COPY McpRag.TestClient/*.csproj ./McpRag.TestClient/
COPY McpRag.slnx ./

# Restore NuGet packages
RUN dotnet restore McpRag/McpRag.csproj

# Copy the source code
COPY McpRag/ ./McpRag/
COPY McpRag.Tests/ ./McpRag.Tests/
COPY McpRag.IntegrationTests/ ./McpRag.IntegrationTests/
COPY McpRag.TestClient/ ./McpRag.TestClient/

# Build the application
RUN dotnet build McpRag/McpRag.csproj -c Release -o /app/build

# Stage 2: Publish the application
FROM build AS publish
RUN dotnet publish McpRag/McpRag.csproj -c Release -o /app/publish /p:UseAppHost=true

# Stage 3: Runtime image
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

# Copy published files from build stage
COPY --from=publish /app/publish .

# Create directory for logs
RUN mkdir -p logs

# Expose necessary ports (if needed for future enhancements)
EXPOSE 5000 8080

# Set environment variables with default values
ENV OLLAMA_HOST=http://ollama:11434
ENV CHROMADB_HOST=http://chromadb:8000
ENV DOTNET_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "McpRag.dll"]