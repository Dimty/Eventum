using Eventum.Shared.Contracts;

namespace Eventum.Bookings.Application.Interfaces;

public interface IBookingEventPublisher
{
    Task PublishAsync(BookingConfirmed message, CancellationToken token = default);
}
