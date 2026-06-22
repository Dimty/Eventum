using Eventum.Domain.Models;

namespace Eventum.Application.Interfaces.Services;

public interface IBookingService
{
     Task<Booking> CreateBookingAsync(Guid eventId, Guid userId);
     
     Task<Booking> GetBookingByIdAsync(Guid bookingId);
     
     Task<bool> DeleteBookingAsync(Guid bookingId, Guid userId);
     
     Task<bool> CancelBookingAsync(Guid bookingId, Guid userId);
}