using System;

namespace McpRag;

/// <summary>
/// Represents the content of a file with metadata.
/// </summary>
public class FileContent
{
    public string Path { get; set; }
    public string FileName { get; set; }
    public string Extension { get; set; }
    public string Content { get; set; }
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
}