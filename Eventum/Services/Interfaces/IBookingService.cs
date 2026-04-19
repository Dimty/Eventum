using Eventum.Models;

namespace Eventum.Services.Interfaces;

public interface IBookingService
{
     Task<Booking> CreateBookingAsync(Guid eventId);
     
     Task<Booking> GetBookingByIdAsync(Guid bookingId);
}