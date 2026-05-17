using Eventum.DataAccess.Contexts;
using Eventum.DTO;
using Eventum.Exceptions;
using Eventum.Models;
using Eventum.Services;
using Eventum.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Eventum.Tests;

public class BookingServiceTests
{
    private readonly ServiceProvider _provider;

    public BookingServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();

        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<BookingService>();
        
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IBookingProcessingService, BookingService>();

        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        
        _provider = services.BuildServiceProvider();
    }

    private async Task<Guid> CreateEventAsync(IServiceScope scope, int totalSeats = 5)
    {
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        var ev = await eventService.CreateAsync(new CreateEventDto
        {
            Title = "Test",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddDays(1),
            TotalSeats = totalSeats
        });

        return ev.Id;
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldReturnPending()
    {
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var evId = await CreateEventAsync(scope);
        
        var booking = await bookingService.CreateBookingAsync(evId);
        
        Assert.Equal(BookingStatus.Pending, booking.Status);
    }
    
    [Fact]
    public async Task CreateBookingAsync_ShouldCreateUniqueBooking()
    {
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var evId = await CreateEventAsync(scope);
        
        var booking1 = await bookingService.CreateBookingAsync(evId);
        var booking2 = await bookingService.CreateBookingAsync(evId);
        
        Assert.NotEqual(booking1.Id, booking2.Id);
    }
    
    [Fact]
    public async Task GetBookingByIdAsync_ShouldReturnBooking()
    {
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        
        var evId = await CreateEventAsync(scope);
        
        var booking = await bookingService.CreateBookingAsync(evId);
        var result = await bookingService.GetBookingByIdAsync(booking.Id);
        
        Assert.Equal(booking.Id, result.Id);
    }
    
    [Fact]
    public async Task GetBookingByIdAsync_ShouldThrow_IfNotFound()
    {
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        
        await Assert.ThrowsAsync<NotFoundException>(() =>
           bookingService.GetBookingByIdAsync(Guid.NewGuid()));
    }
    
    [Fact]
    public async Task CreateBookingAsync_ShouldThrow_IfEventNotFound()
    {
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        
        await Assert.ThrowsAsync<NotFoundException>(() =>
            bookingService.CreateBookingAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Booking_ShouldReflectStatusChange()
    {
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        
        var evId = await CreateEventAsync(scope);
        var booking = await bookingService.CreateBookingAsync(evId);

        booking.Status = BookingStatus.Confirmed;
        booking.ProcessedAt = DateTime.UtcNow;

        var result = await bookingService.GetBookingByIdAsync(booking.Id);

        Assert.Equal(BookingStatus.Confirmed, result.Status);
        Assert.NotNull(result.ProcessedAt);
    }

    [Fact]
    public async Task CreateBooking_ShouldDecreaseAvailableSeats()
    {
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        
        var ev = await CreateEventAsync(scope);

        await bookingService.CreateBookingAsync(ev);

        var updated = (await eventService.GetByIdAsync(ev))!;
        Assert.Equal(4, updated.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookings_UntilLimit_ShouldAllSucceed()
    {
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        
        var ev = await CreateEventAsync(scope, 3);

        var b1 = await bookingService.CreateBookingAsync(ev);
        var b2 = await bookingService.CreateBookingAsync(ev);
        var b3 = await bookingService.CreateBookingAsync(ev);

        Assert.NotEqual(b1.Id, b2.Id);
        Assert.NotEqual(b2.Id, b3.Id);
        Assert.NotEqual(b1.Id, b3.Id);

        var updated = (await eventService.GetByIdAsync(ev))!;
        Assert.Equal(0, updated.AvailableSeats);
    }
    
    [Fact]
    public async Task CreateBooking_WhenEventNotFound_ShouldThrow()
    {
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        
        await Assert.ThrowsAsync<NotFoundException>(() =>
            bookingService.CreateBookingAsync(Guid.NewGuid()));
    }
    
    [Fact]
    public async Task CreateBooking_WhenNoSeats_ShouldThrowNoAvailableSeats()
    {
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        
        var ev = await CreateEventAsync(scope, 1);

        await bookingService.CreateBookingAsync(ev);

        await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
            bookingService.CreateBookingAsync(ev));
    }
    
    [Fact]
    public async Task BookingConfirm_ShouldSetStatusAndProcessedAt()
    {
        using var scope = _provider.CreateScope();
        
        var ev = await CreateEventAsync(scope);

        var booking = new Booking(ev);

        booking.Confirm();

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }
    
    [Fact]
    public void BookingReject_ShouldSetStatusAndProcessedAt()
    {
        var booking = new Booking(Guid.NewGuid());

        booking.Reject();

        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }
    
    [Fact]
    public async Task Reject_ShouldReleaseSeats()
    {
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        
        var guid = await CreateEventAsync(scope, 1);
        var ev = (await eventService.GetByIdAsync(guid))!;
        var booking = await bookingService.CreateBookingAsync(guid);

        booking.Reject();
        ev.ReleaseSeats();

        Assert.Equal(1, ev.AvailableSeats);
    }
    
    [Fact]
    public async Task AfterReject_ShouldAllowNewBooking()
    {
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        
        var guid = await CreateEventAsync(scope, 1);
        var ev = (await eventService.GetByIdAsync(guid))!;

        var booking = await bookingService.CreateBookingAsync(guid);

        booking.Reject();
        ev.ReleaseSeats();

        var newBooking = await bookingService.CreateBookingAsync(guid);

        Assert.NotNull(newBooking);
    }
    
    [Fact]
    public async Task ConcurrentBooking_ShouldNotOverbook()
    {
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        
        var ev = await CreateEventAsync(scope, 5);

        var tasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    return await bookingService.CreateBookingAsync(ev);
                }
                catch (NoAvailableSeatsException)
                {
                    return null;
                }
            }));

        var results = await Task.WhenAll(tasks);

        var success = results.Count(r => r != null);
        var failed = results.Count(r => r == null);

        var updated = (await eventService.GetByIdAsync(ev))!;

        Assert.Equal(5, success);
        Assert.Equal(15, failed);
        Assert.Equal(0, updated.AvailableSeats);
    }

    [Fact]
    public async Task RemoveBooking_WhenEventWasDeleted_ShouldThrowNotFoundException()
    {
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<BookingService>();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        
        var ev = await CreateEventAsync(scope, 3);
        
        var booking = await bookingService.CreateBookingAsync(ev);
        
        await eventService.DeleteAsync(ev);
        
        await Assert.ThrowsAsync<NotFoundException>(() => bookingService.ProcessBookingAsync(booking.Id, TestContext.Current.CancellationToken));
    }
    
    [Fact]
    public async Task ConcurrentBooking_ShouldHaveUniqueIds()
    {
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<BookingService>();
        
        var ev = await CreateEventAsync(scope, 10);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(async () => await bookingService.CreateBookingAsync(ev)));

        var results = await Task.WhenAll(tasks);

        var ids = results.Select(b => b.Id).ToList();

        Assert.Equal(10, ids.Distinct().Count());
    }
}