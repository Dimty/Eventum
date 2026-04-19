using Eventum.Exceptions;
using Eventum.Models;
using Eventum.Services;
using Microsoft.Extensions.Logging;

namespace Eventum.Tests;

public class BookingServiceTests
{
    private readonly BookingService _bookingService;
    private readonly EventService _eventService = new();
    
    public BookingServiceTests()
    {
        _bookingService = new(_eventService);
    }

    private Guid CreateEvent(int totalSeats = 5)
    {
        var ev = _eventService.Create(new Event
        {
            Title = "Test",
            StartAt = DateTime.Now,
            EndAt =  DateTime.Now.AddDays(1),
            TotalSeats = totalSeats,
            AvailableSeats = totalSeats
        });
        
        return ev.Id;
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldReturnPending()
    {
        var evId = CreateEvent();
        
        var booking = await _bookingService.CreateBookingAsync(evId);
        
        Assert.Equal(BookingStatus.Pending, booking.Status);
    }
    
    [Fact]
    public async Task CreateBookingAsync_ShouldCreateUniqueBooking()
    {
        var evId = CreateEvent();
        
        var booking1 = await _bookingService.CreateBookingAsync(evId);
        var booking2 = await _bookingService.CreateBookingAsync(evId);
        
        Assert.NotEqual(booking1.Id, booking2.Id);
    }
    
    [Fact]
    public async Task GetBookingByIdAsync_ShouldReturnBooking()
    {
        var evId = CreateEvent();
        var booking = await _bookingService.CreateBookingAsync(evId);
        
        var result = await _bookingService.GetBookingByIdAsync(booking.Id);
        
        Assert.Equal(booking.Id, result.Id);
    }
    
    [Fact]
    public async Task GetBookingByIdAsync_ShouldThrow_IfNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _bookingService.GetBookingByIdAsync(Guid.NewGuid()));
    }
    
    [Fact]
    public async Task CreateBookingAsync_ShouldThrow_IfEventNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _bookingService.CreateBookingAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Booking_ShouldReflectStatusChange()
    {
        var evId = CreateEvent();
        var booking = await _bookingService.CreateBookingAsync(evId);

        booking.Status = BookingStatus.Confirmed;
        booking.ProcessedAt = DateTime.UtcNow;

        var result = await _bookingService.GetBookingByIdAsync(booking.Id);

        Assert.Equal(BookingStatus.Confirmed, result.Status);
        Assert.NotNull(result.ProcessedAt);
    }

    [Fact]
    public async Task CreateBooking_ShouldDecreaseAvailableSeats()
    {
        var ev = CreateEvent(5);

        await _bookingService.CreateBookingAsync(ev);

        var updated = _eventService.GetById(ev);
        Assert.Equal(4, updated.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookings_UntilLimit_ShouldAllSucceed()
    {
        var ev = CreateEvent(3);

        var b1 = await _bookingService.CreateBookingAsync(ev);
        var b2 = await _bookingService.CreateBookingAsync(ev);
        var b3 = await _bookingService.CreateBookingAsync(ev);

        Assert.NotEqual(b1.Id, b2.Id);
        Assert.NotEqual(b2.Id, b3.Id);
        Assert.NotEqual(b1.Id, b3.Id);

        var updated = _eventService.GetById(ev);
        Assert.Equal(0, updated.AvailableSeats);
    }
    
}