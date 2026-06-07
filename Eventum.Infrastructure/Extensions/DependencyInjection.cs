using Eventum.Application.Common;
using Eventum.Application.Interfaces.Repositories;
using Eventum.Application.Interfaces.Services;
using Eventum.Application.Services;
using Eventum.Infrastructure.Background;
using Eventum.Infrastructure.Data.Contexts;
using Eventum.Infrastructure.Data.Repositories;
using Eventum.Infrastructure.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Eventum.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<BookingService>();
        services.AddScoped<IBookingService>(sp => sp.GetRequiredService<BookingService>());
        services.AddScoped<IBookingProcessingService>(sp => sp.GetRequiredService<BookingService>());
        services.AddHostedService<BookingProcessingService>();
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped(typeof(IAppLogger<>), typeof(AppLoggerAdapter<>));
        
        
        return services;
    }
    
    public static void ApplyMigrations(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.Migrate();
    }
}