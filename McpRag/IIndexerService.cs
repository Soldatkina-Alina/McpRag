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
    
    /// <summary>
    /// Loads and splits a single document into chunks.
    /// </summary>
    /// <param name="filePath">Path to the document file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of document chunks.</returns>
    Task<List<DocumentChunk>> LoadAndSplitDocumentAsync(string filePath, CancellationToken ct = default);
}
