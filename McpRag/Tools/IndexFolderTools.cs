using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.Threading;
using System.Threading.Tasks;
using McpRag;

public class IndexFolderTools
{
    private readonly ILogger<IndexFolderTools> _logger;
    private readonly IIndexerService _indexerService;

    public IndexFolderTools(ILogger<IndexFolderTools> logger, IIndexerService indexerService)
    {
        _logger = logger;
        _indexerService = indexerService;
    }

    [McpServerTool]
    [Description("Counts the number of files in a specified folder that match a given pattern.")]
    public async Task<string> IndexFolder(
        [Description("Path to the folder to search")] string folderPath,
        [Description("File search pattern (default: *.*)")] string pattern = "*.*")
    {
        _logger.LogInformation("IndexFolder tool called with folder: {FolderPath}, pattern: {Pattern}", folderPath, pattern);

        if (!Directory.Exists(folderPath))
        {
            _logger.LogError("Folder not found: {FolderPath}", folderPath);
            return $"Ошибка: папка '{folderPath}' не найдена";
        }

        try
        {
            var files = await _indexerService.LoadFilesAsync(folderPath, pattern, CancellationToken.None);
            
            if (files.Count == 0)
            {
                _logger.LogInformation("No files found in folder {FolderPath} matching pattern {Pattern}", folderPath, pattern);
                return $"Не найдено файлов, соответствующих паттерну {pattern}";
            }

            var totalSize = files.Sum(f => f.Size);
            _logger.LogInformation("Loaded {FileCount} files in folder {FolderPath} matching pattern {Pattern}, total size {TotalSize} characters", files.Count, folderPath, pattern, totalSize);

            return $"Загружено {files.Count} файлов, общий размер {totalSize} символов";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when indexing folder {FolderPath}: {Message}", folderPath, ex.Message);
            return $"Ошибка: {ex.Message}";
        }
    }
}