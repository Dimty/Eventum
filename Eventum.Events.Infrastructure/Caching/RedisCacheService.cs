using Eventum.Events.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Eventum.Events.Infrastructure.Caching;

public class RedisCacheService(
    IConnectionMultiplexer connectionMultiplexer,
    ILogger<RedisCacheService> logger) : ICacheService
{
    public async Task<string?> GetStringAsync(string key, CancellationToken token = default)
    {
        try
        {
            var database = connectionMultiplexer.GetDatabase();
            return await database.StringGetAsync(key);
        }
        catch (Exception ex) when (IsRedisFailure(ex))
        {
            logger.LogWarning(ex, "Redis get failed. Key={Key}", key);
            return null;
        }
    }

    public async Task SetStringAsync(string key, string value, TimeSpan ttl, CancellationToken token = default)
    {
        try
        {
            var database = connectionMultiplexer.GetDatabase();
            await database.StringSetAsync(key, value, ttl);
        }
        catch (Exception ex) when (IsRedisFailure(ex))
        {
            logger.LogWarning(ex, "Redis set failed. Key={Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        try
        {
            var database = connectionMultiplexer.GetDatabase();
            await database.KeyDeleteAsync(key);
        }
        catch (Exception ex) when (IsRedisFailure(ex))
        {
            logger.LogWarning(ex, "Redis remove failed. Key={Key}", key);
        }
    }

    private static bool IsRedisFailure(Exception ex) =>
        ex is RedisException or TimeoutException or InvalidOperationException or ObjectDisposedException;
}
