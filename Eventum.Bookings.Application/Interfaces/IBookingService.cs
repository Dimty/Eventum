using Eventum.Bookings.Domain;

namespace Eventum.Bookings.Application.Interfaces;

public interface IBookingService
{
    Task<Booking> CreateBookingAsync(Guid eventId, Guid userId, int seats = 1, CancellationToken token = default);

    Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken token = default);

    Task CancelBookingAsync(Guid bookingId, Guid userId, CancellationToken token = default);
}
