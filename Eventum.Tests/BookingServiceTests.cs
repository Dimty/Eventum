using Eventum.Exceptions;
using Eventum.Models;
using Eventum.Services;

namespace Eventum.Tests;

public class BookingServiceTests
{
    private readonly BookingService _bookingService;
    private readonly EventService _eventService = new();
    
    public BookingServiceTests()
    {
        _bookingService = new(_eventService);
    }

    private Guid CreateEvent()
    {
        var ev = _eventService.Create(new Event
        {
            Title = "Test",
            StartAt = DateTime.Now,
            EndAt =  DateTime.Now.AddDays(1),
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
}