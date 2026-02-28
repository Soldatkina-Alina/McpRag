using System.ComponentModel;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;

/// <summary>
/// Echo tool for demonstration purposes.
/// Returns the input message prefixed with "Echo: ".
/// </summary>
internal class EchoTools
{
    private readonly ILogger<EchoTools> _logger;

    public EchoTools(ILogger<EchoTools> logger)
    {
        _logger = logger;
    }

    [McpServerTool]
    [Description("Echoes back the input message prefixed with 'Echo: '.")]
    public string Echo(
        [Description("Message to echo")] string message)
    {
        _logger.LogInformation("Echo tool called with message: {Message}", message);
        return $"Echo: {message}";
    }
}