using Eventum.Models;
using Eventum.Services;
using Eventum.Services.Interfaces;

namespace Eventum.Background;

public class BookingProcessingService(IServiceScopeFactory serviceScopeFactory): BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private const int CustomDelay = 1000;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IEnumerable<Guid> pendingIds;

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var bookingService = scope.ServiceProvider
                    .GetRequiredService<IBookingProcessingService>();

                pendingIds = (await bookingService.GetPendingBookingIdsAsync()).ToList();
            }

            var tasks = pendingIds.Select(id => Task.Run(async () =>
            {
                using var innerScope = _serviceScopeFactory.CreateScope();

                var bookingService = innerScope.ServiceProvider
                    .GetRequiredService<IBookingProcessingService>();

                await bookingService.ProcessBookingAsync(id, stoppingToken);

            }, stoppingToken));

            await Task.WhenAll(tasks);

            await Task.Delay(CustomDelay, stoppingToken);
        }
    }
}