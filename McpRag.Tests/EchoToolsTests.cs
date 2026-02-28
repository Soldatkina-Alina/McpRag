using McpRag.Tools;
using Microsoft.Extensions.Logging;
using Xunit;

namespace McpRag.Tests;

public class EchoToolsTests
{
    [Fact]
    public void Echo_ShouldReturnMessage_WithPrefix()
    {
        // Arrange
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<EchoTools>();
        var echoTools = new EchoTools(logger);
        var testMessage = "Привет, мир!";

        // Act
        var result = echoTools.Echo(testMessage);

        // Assert
        Assert.Equal($"Echo: {testMessage}", result);
    }

    [Fact]
    public void Echo_ShouldReturnEmptyMessage_WhenEmptyString()
    {
        // Arrange
        var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger<EchoTools>();
        var echoTools = new EchoTools(logger);

        // Act
        var result = echoTools.Echo(string.Empty);

        // Assert
        Assert.Equal("Echo: ", result);
    }
}