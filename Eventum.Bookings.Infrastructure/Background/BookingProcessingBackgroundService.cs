using Eventum.Bookings.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Eventum.Bookings.Infrastructure.Background;

public class BookingProcessingBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<BookingProcessingBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan PollDelay = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IReadOnlyCollection<Guid> pendingIds;

            using (var scope = serviceScopeFactory.CreateScope())
            {
                var processor = scope.ServiceProvider.GetRequiredService<IBookingProcessingService>();
                pendingIds = await processor.GetPendingBookingIdsAsync(stoppingToken);
            }

            var tasks = pendingIds.Select(id => ProcessOneAsync(id, stoppingToken));
            await Task.WhenAll(tasks);

            await Task.Delay(PollDelay, stoppingToken);
        }
    }

    private async Task ProcessOneAsync(Guid bookingId, CancellationToken token)
    {
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IBookingProcessingService>();
            await processor.ProcessBookingAsync(bookingId, token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Booking processing failed. BookingId={BookingId}", bookingId);
        }
    }
}
