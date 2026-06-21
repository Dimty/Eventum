using Eventum.Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Eventum.Infrastructure.Background;

public class BookingProcessingService(IServiceScopeFactory serviceScopeFactory): BackgroundService
{
    private const int CustomDelay = 1000;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IEnumerable<Guid> pendingIds;

            using (var scope = serviceScopeFactory.CreateScope())
            {
                var bookingService = scope.ServiceProvider
                    .GetRequiredService<IBookingProcessingService>();

                pendingIds = (await bookingService.GetPendingBookingIdsAsync()).ToList();
            }

            var tasks = pendingIds.Select(id => Task.Run(async () =>
            {
                using var innerScope = serviceScopeFactory.CreateScope();

                var bookingService = innerScope.ServiceProvider
                    .GetRequiredService<IBookingProcessingService>();

                await bookingService.ProcessBookingAsync(id, stoppingToken);

            }, stoppingToken));

            await Task.WhenAll(tasks);

            await Task.Delay(CustomDelay, stoppingToken);
        }
    }
}