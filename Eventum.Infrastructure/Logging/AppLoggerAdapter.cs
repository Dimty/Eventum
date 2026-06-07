using Eventum.Application.Common;
using Microsoft.Extensions.Logging;

namespace Eventum.Infrastructure.Logging;

public class AppLoggerAdapter<T> : IAppLogger<T>
{
    private readonly ILogger<T> _logger;

    public AppLoggerAdapter(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<T>();
    }

    public void LogInformation(string message, params object?[] args)
        => _logger.LogInformation(message, args);

    public void LogWarning(string message, params object?[] args)
        => _logger.LogWarning(message, args);

    public void LogError(Exception? exception, string message, params object?[] args)
        => _logger.LogError(exception, message, args);
}