using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

internal class IndexFolderTools
{
    private readonly ILogger<IndexFolderTools> _logger;

    public IndexFolderTools(ILogger<IndexFolderTools> logger)
    {
        _logger = logger;
    }

    [McpServerTool]
    [Description("Counts the number of files in a specified folder that match a given pattern.")]
    public string IndexFolder(
        [Description("Path to the folder to search")] string folderPath,
        [Description("File search pattern (default: *.*)")] string pattern = "*.*")
    {
        _logger.LogInformation("IndexFolder tool called with folder: {FolderPath}, pattern: {Pattern}", folderPath, pattern);

        if (!Directory.Exists(folderPath))
        {
            _logger.LogError("Folder not found: {FolderPath}", folderPath);
            return $"Error: Folder '{folderPath}' not found";
        }

        try
        {
            var files = Directory.GetFiles(folderPath, pattern, SearchOption.TopDirectoryOnly);
            _logger.LogInformation("Found {FileCount} files in folder {FolderPath} matching pattern {Pattern}", files.Length, folderPath, pattern);

            return $"Найдено {files.Length} файлов в папке {folderPath}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when indexing folder {FolderPath}: {Message}", folderPath, ex.Message);
            return $"Error: {ex.Message}";
        }
    }
}