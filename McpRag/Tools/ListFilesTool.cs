using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using McpRag;

public class ListFilesTool
{
    private readonly ILogger<ListFilesTool> _logger;
    private readonly IIndexerService _indexerService;

    public ListFilesTool(ILogger<ListFilesTool> logger, IIndexerService indexerService)
    {
        _logger = logger;
        _indexerService = indexerService;
    }

    [McpServerTool]
    [Description("Lists the loaded files with optional extension filtering.")]
    public string ListFiles(
        [Description("Filter files by extension (e.g., .txt, .cs)")] string extension = null)
    {
        try
        {
            _logger.LogInformation("ListFiles tool called with extension: {Extension}", extension);

            var loadedFiles = _indexerService.GetLoadedFiles();
            
            if (loadedFiles.Count == 0)
            {
                _logger.LogInformation("No files loaded");
                return "Нет загруженных файлов";
            }

            var filteredFiles = loadedFiles;
            
            if (!string.IsNullOrEmpty(extension))
            {
                if (!extension.StartsWith("."))
                    extension = "." + extension;
                    
                extension = extension.ToLower();
                filteredFiles = loadedFiles.Where(f => f.Extension.ToLower() == extension).ToList();
                
                if (filteredFiles.Count == 0)
                {
                    _logger.LogInformation("No files found with extension: {Extension}", extension);
                    return $"Нет файлов с расширением {extension}";
                }
            }

            var fileDescriptions = filteredFiles.Select(f => 
                $"{f.FileName} ({FormatSize(f.Size)}, {f.LastModified.ToString("yyyy-MM-dd")})");

            return string.Join(", ", fileDescriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ListFiles tool");
            return $"Ошибка: {ex.Message}";
        }
    }

    private string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        
        return $"{size:0.0} {sizes[order]}";
    }
}