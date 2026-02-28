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
/// Implementation of IIndexerService for loading files from disk.
/// </summary>
public class IndexerService : IIndexerService
{
    private readonly IndexerConfig _config;
    private readonly ILogger<IndexerService> _logger;
    private List<FileContent> _loadedFiles = new();

    public IndexerService(IOptions<IndexerConfig> config, ILogger<IndexerService> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading files from folder: {FolderPath}", folderPath);
            return new List<FileContent>();
        }

        return loadedFiles;
    }

    public List<FileContent> GetLoadedFiles()
    {
        return _loadedFiles;
    }

    public void ClearLoadedFiles()
    {
        _loadedFiles.Clear();
        _logger.LogInformation("Cleared loaded files");
    }
}