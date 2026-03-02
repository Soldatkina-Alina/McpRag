using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace McpRag;

/// <summary>
/// Interface for indexer service operations.
/// </summary>
public interface IIndexerService
{
    /// <summary>
    /// Loads files from the specified folder with the given pattern.
    /// </summary>
    Task<List<FileContent>> LoadFilesAsync(string folderPath, string pattern, CancellationToken ct = default);
    
    /// <summary>
    /// Returns the list of loaded files.
    /// </summary>
    List<FileContent> GetLoadedFiles();
    
    /// <summary>
    /// Clears the list of loaded files.
    /// </summary>
    void ClearLoadedFiles();
}
