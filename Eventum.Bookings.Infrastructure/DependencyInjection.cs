using Eventum.Bookings.Application.Interfaces;
using Eventum.Bookings.Infrastructure.Background;
using Eventum.Bookings.Infrastructure.Data;
using Eventum.Bookings.Infrastructure.Kafka;
using Eventum.Bookings.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Eventum.Bookings.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBookingsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaSettings>(configuration.GetSection("Kafka"));
        services.AddDbContext<BookingsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("BookingsConnection")));

        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddSingleton<IBookingEventPublisher, KafkaBookingEventPublisher>();
        services.AddHostedService<BookingProcessingBackgroundService>();

        return services;
    }
}
