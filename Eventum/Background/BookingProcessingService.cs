using Eventum.Models;
using Eventum.Services;
using Eventum.Services.Interfaces;

namespace Eventum.Background;

public class BookingProcessingService(IServiceScopeFactory serviceScopeFactory): BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly Random _random = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingProcessingService>();
            var pending = bookingService!.GetPendingBookings();

            var tasks = pending.Select(b => bookingService.ProcessBookingAsync(b, stoppingToken));

            await Task.WhenAll(tasks);

            await Task.Delay(1000, stoppingToken);
        }
    }
}