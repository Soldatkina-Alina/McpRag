using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace McpRag;

/// <summary>
/// Сервис для индексации файлов из директории.
/// Реализует интерфейс <see cref="IIndexerService"/> и отвечает за загрузку,
/// разбиение на чанки и индексацию контента файлов в векторное хранилище.
/// </summary>
public class IndexerService : IIndexerService
{
    private readonly IndexerConfig _config;
    private readonly IVectorStoreService _vectorStore;
    private readonly IOllamaService _ollama;
    private readonly ILogger<IndexerService> _logger;
    private List<FileContent> _loadedFiles = new();

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="IndexerService"/>.
    /// </summary>
    /// <param name="config">Конфигурация индексера.</param>
    /// <param name="vectorStore">Векторное хранилище для хранения эмбеддингов.</param>
    /// <param name="ollama">Сервис для работы с Ollama (генерация эмбеддингов).</param>
    /// <param name="logger">Логгер для записи сообщений.</param>
    public IndexerService(IOptions<IndexerConfig> config, IVectorStoreService vectorStore, 
        IOllamaService ollama, ILogger<IndexerService> logger)
    {
        _config = config.Value;
        _vectorStore = vectorStore;
        _ollama = ollama;
        _logger = logger;
    }

    /// <summary>
    /// Загружает файлы из указанной директории по заданному паттерну и индексирует их.
    /// </summary>
    /// <param name="folderPath">Путь к директории для сканирования.</param>
    /// <param name="pattern">Паттерн поиска файлов (например, "*.txt").</param>
    /// <param name="ct">Токен отмены для отмены операции.</param>
    /// <returns>Список загруженных файлов.</returns>
    public async Task<List<FileContent>> LoadFilesAsync(string folderPath, string pattern, CancellationToken ct = default)
    {
        _logger.LogInformation("Loading files from folder: {FolderPath} with pattern: {Pattern}", folderPath, pattern);
        
        // Clear previously loaded files
        _loadedFiles.Clear();
        
        if (!Directory.Exists(folderPath))
        {
            _logger.LogWarning("Folder not found: {FolderPath}", folderPath);
            return new List<FileContent>();
        }

        var loadedFiles = new List<FileContent>();
        var skippedFiles = 0;

        try
        {
            var files = Directory.GetFiles(folderPath, pattern, SearchOption.AllDirectories);
            _logger.LogInformation("Found {Count} files matching pattern", files.Length);

            foreach (var filePath in files)
            {
                if (ct.IsCancellationRequested)
                    break;

                var fileInfo = new FileInfo(filePath);
                
                // Check if file extension is supported
                if (!_config.SupportedExtensions.Contains(fileInfo.Extension.ToLower()))
                {
                    _logger.LogDebug("Skipping file with unsupported extension: {FilePath}", filePath);
                    skippedFiles++;
                    continue;
                }

                // Check file size
                if (fileInfo.Length > _config.MaxFileSizeMB * 1024 * 1024)
                {
                    _logger.LogWarning("Skipping large file: {FilePath} ({Size} bytes)", filePath, fileInfo.Length);
                    skippedFiles++;
                    continue;
                }

                try
                {
                    _logger.LogDebug("Reading file: {FilePath}", filePath);
                    var content = await File.ReadAllTextAsync(filePath, ct);
                    
                    var fileContent = new FileContent
                    {
                        Path = filePath,
                        FileName = fileInfo.Name,
                        Extension = fileInfo.Extension,
                        Content = content,
                        Size = content.Length,
                        LastModified = fileInfo.LastWriteTime
                    };

                    loadedFiles.Add(fileContent);
                }
                catch (IOException ex) when (_config.SkipLockedFiles && (ex.HResult == -2147024864 || ex.HResult == -2147024866)) // File is locked
                {
                    _logger.LogWarning("Skipping locked file: {FilePath}", filePath);
                    skippedFiles++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading file: {FilePath}", filePath);
                    if (!_config.SkipLockedFiles)
                        throw;
                    skippedFiles++;
                }
            }

            _loadedFiles = loadedFiles;
            _logger.LogInformation("Loaded {Count} files, skipped {Skipped} files", loadedFiles.Count, skippedFiles);
            
            // Process and index files
            await IndexFilesAsync(loadedFiles, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading files from folder: {FolderPath}", folderPath);
            return new List<FileContent>();
        }

        return loadedFiles;
    }

    /// <summary>
    /// Индексирует загруженные файлы: разбивает на чанки, генерирует эмбеддинги и сохраняет в векторное хранилище.
    /// </summary>
    /// <param name="files">Список файлов для индексации.</param>
    /// <param name="ct">Токен отмены для отмены операции.</param>
    private async Task IndexFilesAsync(List<FileContent> files, CancellationToken ct = default)
    {
        _logger.LogInformation("Indexing {Count} files", files.Count);
        
        var chunks = new List<DocumentChunk>();
        
        foreach (var file in files)
        {
            // Split file into chunks
            var fileChunks = SplitDocumentIntoChunks(file.Content);
            
            foreach (var chunk in fileChunks)
            {
                // Generate embedding for each chunk
                var embedding = await _ollama.GenerateEmbeddingsAsync(chunk, ct);
                
                chunks.Add(new DocumentChunk
                {
                    Text = chunk,
                    Source = file.Path,
                    ChunkIndex = fileChunks.IndexOf(chunk),
                    Embedding = embedding,
                    Metadata = new Dictionary<string, object>
                    {
                        ["file_name"] = file.FileName,
                        ["extension"] = file.Extension,
                        ["size"] = file.Size
                    }
                });
            }
        }
        
        // Add to vector store
        await _vectorStore.AddDocumentsAsync(chunks, ct);
        _logger.LogInformation("Indexed {Count} document chunks", chunks.Count);
    }

    /// <summary>
    /// Разбивает документ на чанки с поддержкой перекрытия.
    /// </summary>
    /// <param name="text">Текст для разбиения.</param>
    /// <param name="chunkSize">Размер каждого чанка (по умолчанию 1000 символов).</param>
    /// <param name="chunkOverlap">Перекрытие между чанками (по умолчанию 200 символов).</param>
    /// <returns>Список чанков документа.</returns>
    private List<string> SplitDocumentIntoChunks(string text, int chunkSize = 1000, int chunkOverlap = 200)
    {
        var chunks = new List<string>();
        int start = 0;
        int textLength = text.Length;
        int lastChunkStart = 0;
        
        while (start < textLength)
        {
            int end = Math.Min(start + chunkSize, textLength);
            
            // Find sentence boundary for cleaner chunks
            if (end < textLength)
            {
                int lastPeriod = text.LastIndexOf('.', end, end - start);
                int lastNewline = text.LastIndexOf('\n', end, end - start);
                
                if (lastPeriod > start + chunkSize / 2)
                {
                    end = lastPeriod + 1;
                }
                else if (lastNewline > start + chunkSize / 2)
                {
                    end = lastNewline + 1;
                }
            }
            
            chunks.Add(text.Substring(start, end - start).Trim());
            
            start = end - chunkOverlap;
            if (start <= lastChunkStart) // Avoid infinite loop
            {
                start = end;
            }
            
            lastChunkStart = end;
        }
        
        return chunks;
    }

    /// <summary>
    /// Возвращает список загруженных файлов.
    /// </summary>
    /// <returns>Список загруженных файлов.</returns>
    public List<FileContent> GetLoadedFiles()
    {
        return _loadedFiles;
    }

    /// <summary>
    /// Очищает список загруженных файлов.
    /// </summary>
    public void ClearLoadedFiles()
    {
        _loadedFiles.Clear();
        _logger.LogInformation("Cleared loaded files");
    }
}