using Eventum.Events.Application.Interfaces;
using Eventum.Events.Application.Options;
using Eventum.Events.Infrastructure.Caching;
using Eventum.Events.Infrastructure.Data;
using Eventum.Events.Infrastructure.Kafka;
using Eventum.Events.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Eventum.Events.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEventsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaSettings>(configuration.GetSection("Kafka"));
        var redisSettings = configuration.GetSection("Redis").Get<EventCacheSettings>() ?? new EventCacheSettings();
        services.AddSingleton(redisSettings);
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var redisConfiguration = ConfigurationOptions.Parse(redisSettings.ConnectionString);
            redisConfiguration.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(redisConfiguration);
        });
        services.AddSingleton<ICacheService, RedisCacheService>();

        services.AddDbContext<EventsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("EventsConnection")));

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddHostedService<KafkaTopicInitializer>();
        services.AddHostedService<BookingConfirmedConsumer>();

        return services;
    }
}
