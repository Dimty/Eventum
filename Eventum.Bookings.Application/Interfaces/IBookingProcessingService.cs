namespace Eventum.Bookings.Application.Interfaces;

public interface IBookingProcessingService
{
    Task<IReadOnlyCollection<Guid>> GetPendingBookingIdsAsync(CancellationToken token = default);

    Task ProcessBookingAsync(Guid bookingId, CancellationToken token = default);
}
