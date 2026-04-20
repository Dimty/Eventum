using Eventum.DTO;
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
        var ev = _eventService.Create(new CreateEventDto()
        {
            Title = "Test",
            StartAt = DateTime.Now,
            EndAt =  DateTime.Now.AddDays(1),
            TotalSeats = totalSeats,
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
    
    [Fact]
    public async Task CreateBooking_WhenEventNotFound_ShouldThrow()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _bookingService.CreateBookingAsync(Guid.NewGuid()));
    }
    
    [Fact]
    public async Task CreateBooking_WhenNoSeats_ShouldThrowNoAvailableSeats()
    {
        var ev = CreateEvent(1);

        await _bookingService.CreateBookingAsync(ev);

        await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
            _bookingService.CreateBookingAsync(ev));
    }
    
    [Fact]
    public void BookingConfirm_ShouldSetStatusAndProcessedAt()
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        booking.Confirm();

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }
    
    [Fact]
    public void BookingReject_ShouldSetStatusAndProcessedAt()
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        booking.Reject();

        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }
    
    [Fact]
    public async Task Reject_ShouldReleaseSeats()
    {
        var guid = CreateEvent(1);
        var ev = _eventService.GetById(guid);
        var booking = await _bookingService.CreateBookingAsync(guid);

        booking.Reject();
        ev.ReleaseSeats();

        Assert.Equal(1, ev.AvailableSeats);
    }
    
    [Fact]
    public async Task AfterReject_ShouldAllowNewBooking()
    {
        var guid = CreateEvent(1);
        var ev = _eventService.GetById(guid);

        var booking = await _bookingService.CreateBookingAsync(guid);

        booking.Reject();
        ev.ReleaseSeats();

        var newBooking = await _bookingService.CreateBookingAsync(guid);

        Assert.NotNull(newBooking);
    }
    
    [Fact]
    public async Task ConcurrentBooking_ShouldNotOverbook()
    {
        var ev = CreateEvent(5);

        var tasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    return await _bookingService.CreateBookingAsync(ev);
                }
                catch (NoAvailableSeatsException)
                {
                    return null;
                }
            }));

        var results = await Task.WhenAll(tasks);

        var success = results.Count(r => r != null);
        var failed = results.Count(r => r == null);

        var updated = _eventService.GetById(ev);

        Assert.Equal(5, success);
        Assert.Equal(15, failed);
        Assert.Equal(0, updated.AvailableSeats);
    }
    
    [Fact]
    public async Task ConcurrentBooking_ShouldHaveUniqueIds()
    {
        var ev = CreateEvent(10);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _bookingService.CreateBookingAsync(ev));

        var results = await Task.WhenAll(tasks);

        var ids = results.Select(b => b.Id).ToList();

        Assert.Equal(10, ids.Distinct().Count());
    }
}