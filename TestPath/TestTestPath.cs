using System;
using System.IO;

namespace TestPath;

public class TestTestPath
{
    public static void Main()
    {
        var testPaths = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "test_docs"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "test_docs"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "test_docs"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "test_docs"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "test_docs")
        };

        Console.WriteLine("Current directory: " + Directory.GetCurrentDirectory());
        Console.WriteLine();

        foreach (var path in testPaths)
        {
            Console.WriteLine($"Path: {path}");
            Console.WriteLine($"Exists: {Directory.Exists(path)}");
            Console.WriteLine();
        }
    }
}