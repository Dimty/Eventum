namespace Eventum.Events.Application.Interfaces;

public interface ICacheService
{
    Task<string?> GetStringAsync(string key, CancellationToken token = default);

    Task SetStringAsync(string key, string value, TimeSpan ttl, CancellationToken token = default);

    Task RemoveAsync(string key, CancellationToken token = default);
}
