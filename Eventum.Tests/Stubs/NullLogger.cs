using Eventum.Application.Common;

namespace Eventum.Tests.Stubs;

public class NullAppLogger<T> : IAppLogger<T>
{
    public void LogInformation(string message, params object?[] args) { }
    public void LogWarning(string message, params object?[] args) { }
    public void LogError(Exception? exception, string message, params object?[] args) { }
}