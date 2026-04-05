using Eventum.Models;
using Eventum.Services.Interfaces;

namespace Eventum.Services;

public class BookingService(IEventService eventService): IBookingService
{
    private readonly List<Booking> _bookings = new();
    private readonly IEventService _eventService = eventService;

    public Task<Booking> CreateBookingAsync(Guid eventId)
    {
        _eventService.GetById(eventId);
        
        var booking = new Booking
        {
          Id  =  Guid.NewGuid(),
          EventId = eventId,
          Status =  BookingStatus.Pending,
          CreatedAt =  DateTime.UtcNow
        };
        
        _bookings.Add(booking);
        
        return Task.FromResult(booking);
    }

    public Task<Booking> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId);

        if (booking == null)
            throw new KeyNotFoundException($"Booking {bookingId} not found");

        return Task.FromResult(booking);
    }
    
    public IEnumerable<Booking> GetPendingBookings()
    {
        return _bookings.Where(b => b.Status == BookingStatus.Pending).ToList();
    }
}