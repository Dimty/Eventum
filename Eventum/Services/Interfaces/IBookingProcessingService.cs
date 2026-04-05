using Eventum.Models;

namespace Eventum.Services.Interfaces;

public interface IBookingProcessingService
{
    Task ProcessBookingAsync(Booking booking, CancellationToken token);
    IEnumerable<Booking> GetPendingBookings();
}