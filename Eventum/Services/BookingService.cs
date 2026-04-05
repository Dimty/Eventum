using System.Collections.Concurrent;
using Eventum.Exceptions;
using Eventum.Models;
using Eventum.Services.Interfaces;

namespace Eventum.Services;

public class BookingService(IEventService eventService): IBookingService
{
    private readonly ConcurrentDictionary<Guid, Booking> _bookings = new();
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
        
        _bookings[booking.Id] = booking;
        
        return Task.FromResult(booking);
    }

    public Task<Booking> GetBookingByIdAsync(Guid bookingId)
    {
        if (!_bookings.TryGetValue(bookingId, out var booking))
            throw new NotFoundException($"Booking {bookingId} not found");

        return Task.FromResult(booking);
    }
    
    public IEnumerable<Booking> GetPendingBookings()
    {
        return _bookings.Values.Where(b => b.Status == BookingStatus.Pending).ToList();
    }
}