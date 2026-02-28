using System.Collections.Generic;

namespace McpRag;

/// <summary>
/// Configuration class for indexer service settings.
/// </summary>
public class IndexerConfig
{
    public List<string> SupportedExtensions { get; set; } = 
        new List<string> { ".txt", ".md", ".cs", ".js", ".ts", ".json", ".yaml", ".rst" };
    public int MaxFileSizeMB { get; set; } = 10;
    public bool SkipLockedFiles { get; set; } = true;
}