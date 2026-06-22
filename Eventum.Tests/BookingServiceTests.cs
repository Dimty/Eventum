using Eventum.Application.Common;
using Eventum.Application.DTO;
using Eventum.Application.Exceptions;
using Eventum.Application.Interfaces.Repositories;
using Eventum.Application.Interfaces.Services;
using Eventum.Application.Services;
using Eventum.Domain.Enums;
using Eventum.Domain.Exceptions;
using Eventum.Domain.Models;
using Eventum.Infrastructure.Data.Contexts;
using Eventum.Infrastructure.Data.Repositories;
using Eventum.Tests.Stubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
        
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<BookingService>();
        
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IBookingProcessingService, BookingService>();

        services.AddSingleton(typeof(IAppLogger<>), typeof(NullAppLogger<>));
        
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
        // Arrange
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var evId = await CreateEventAsync(scope);
        
        // Act
        var booking = await bookingService.CreateBookingAsync(evId, Guid.Empty);
        
        // Assert
        Assert.Equal(BookingStatus.Pending, booking.Status);
    }
    
    [Fact]
    public async Task CreateBookingAsync_ShouldCreateUniqueBooking()
    {
        // Arrange
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var evId = await CreateEventAsync(scope);
        
        // Act
        var booking1 = await bookingService.CreateBookingAsync(evId,Guid.Empty);
        var booking2 = await bookingService.CreateBookingAsync(evId,Guid.Empty);
        
        // Assert
        Assert.NotEqual(booking1.Id, booking2.Id);
    }
    
    [Fact]
    public async Task GetBookingByIdAsync_ShouldReturnBooking()
    {
        // Arrange
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var evId = await CreateEventAsync(scope);
        var booking = await bookingService.CreateBookingAsync(evId, Guid.Empty);
        
        // Act
        var result = await bookingService.GetBookingByIdAsync(booking.Id);
        
        // Assert
        Assert.Equal(booking.Id, result.Id);
    }
    
    [Fact]
    public async Task GetBookingByIdAsync_ShouldThrow_IfNotFound()
    {
        // Arrange
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        
        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
           bookingService.GetBookingByIdAsync(Guid.NewGuid()));
    }
    
    [Fact]
    public async Task CreateBookingAsync_ShouldThrow_IfEventNotFound()
    {
        // Arrange
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        
        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            bookingService.CreateBookingAsync(Guid.NewGuid(),Guid.Empty));
    }

    [Fact]
    public async Task Booking_ShouldReflectStatusChange()
    {
        // Arrange
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var evId = await CreateEventAsync(scope);
        var booking = await bookingService.CreateBookingAsync(evId,Guid.Empty);

        // Act
        typeof(Booking).GetProperty(nameof(Booking.Status))!.SetValue(booking,BookingStatus.Confirmed);
        typeof(Booking).GetProperty(nameof(Booking.ProcessedAt))!.SetValue(booking, DateTime.UtcNow);
        //booking.Status = BookingStatus.Confirmed;
        //booking.ProcessedAt = DateTime.UtcNow;

        // Assert
        var result = await bookingService.GetBookingByIdAsync(booking.Id);
        Assert.Equal(BookingStatus.Confirmed, result.Status);
        Assert.NotNull(result.ProcessedAt);
    }

    [Fact]
    public async Task CreateBooking_ShouldDecreaseAvailableSeats()
    {
        // Arrange
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var ev = await CreateEventAsync(scope);

        // Act
        await bookingService.CreateBookingAsync(ev,Guid.Empty);

        // Assert
        var updated = (await eventService.GetByIdAsync(ev))!;
        Assert.Equal(4, updated.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookings_UntilLimit_ShouldAllSucceed()
    {
        // Arrange
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var ev = await CreateEventAsync(scope, 3);

        // Act
        var b1 = await bookingService.CreateBookingAsync(ev,Guid.Empty);
        var b2 = await bookingService.CreateBookingAsync(ev,Guid.Empty);
        var b3 = await bookingService.CreateBookingAsync(ev,Guid.Empty);

        // Assert
        Assert.NotEqual(b1.Id, b2.Id);
        Assert.NotEqual(b2.Id, b3.Id);
        Assert.NotEqual(b1.Id, b3.Id);

        var updated = (await eventService.GetByIdAsync(ev))!;
        Assert.Equal(0, updated.AvailableSeats);
    }
    
    [Fact]
    public async Task CreateBooking_WhenEventNotFound_ShouldThrow()
    {
        // Arrange
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        
        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            bookingService.CreateBookingAsync(Guid.NewGuid(),Guid.Empty));
    }
    
    [Fact]
    public async Task CreateBooking_WhenNoSeats_ShouldThrowNoAvailableSeats()
    {
        // Arrange
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var ev = await CreateEventAsync(scope, 1);
        await bookingService.CreateBookingAsync(ev,Guid.Empty);

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            bookingService.CreateBookingAsync(ev,Guid.Empty));
    }
    
    [Fact]
    public async Task BookingConfirm_ShouldSetStatusAndProcessedAt()
    {
        // Arrange
        using var scope = _provider.CreateScope();
        var ev = await CreateEventAsync(scope);
        var booking = new Booking(ev,Guid.Empty);

        // Act
        booking.Confirm();

        // Assert
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }
    
    [Fact]
    public void BookingReject_ShouldSetStatusAndProcessedAt()
    {
        // Arrange
        var booking = new Booking(Guid.NewGuid(),Guid.Empty);

        // Act
        booking.Reject();

        // Assert
        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }
    
    [Fact]
    public async Task Reject_ShouldReleaseSeats()
    {
        // Arrange
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var guid = await CreateEventAsync(scope, 1);
        var ev = (await eventService.GetByIdAsync(guid))!;
        var booking = await bookingService.CreateBookingAsync(guid,Guid.Empty);

        // Act
        booking.Reject();
        ev.ReleaseSeats();

        // Assert
        Assert.Equal(1, ev.AvailableSeats);
    }
    
    [Fact]
    public async Task AfterReject_ShouldAllowNewBooking()
    {
        // Arrange
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var guid = await CreateEventAsync(scope, 1);
        var ev = (await eventService.GetByIdAsync(guid))!;
        var booking = await bookingService.CreateBookingAsync(guid,Guid.Empty);

        // Act
        booking.Reject();
        ev.ReleaseSeats();
        var newBooking = await bookingService.CreateBookingAsync(guid,Guid.Empty);

        // Assert
        Assert.NotNull(newBooking);
    }
    
    [Fact]
    public async Task ConcurrentBooking_ShouldNotOverbook()
    {
        // Arrange
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var ev = await CreateEventAsync(scope, 5);

        // Act
        var tasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    return await bookingService.CreateBookingAsync(ev,Guid.Empty);
                }
                catch (BusinessRuleViolationException)
                {
                    return null;
                }
            }));

        var results = await Task.WhenAll(tasks);

        // Assert
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
        // Arrange
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<BookingService>();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var ev = await CreateEventAsync(scope, 3);
        var booking = await bookingService.CreateBookingAsync(ev,Guid.Empty);
        await eventService.DeleteAsync(ev);
        
        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => bookingService.ProcessBookingAsync(booking.Id, TestContext.Current.CancellationToken));
    }
    
    [Fact]
    public async Task ConcurrentBooking_ShouldHaveUniqueIds()
    {
        // Arrange
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<BookingService>();
        var ev = await CreateEventAsync(scope, 10);

        // Act
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(async () => await bookingService.CreateBookingAsync(ev,Guid.Empty)));

        var results = await Task.WhenAll(tasks);

        // Assert
        var ids = results.Select(b => b.Id).ToList();
        Assert.Equal(10, ids.Distinct().Count());
    }
    
    
    [Fact]
    public async Task CancelBooking_WhenBookingAlreadyCancelled_ShouldBookingAlreadyCancelled()
    {
        // Arrange
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<BookingService>();
        var ev = await CreateEventAsync(scope, 10);
        var userId = Guid.Empty;
        var booking = await bookingService.CreateBookingAsync(ev,userId);
        await bookingService.CancelBookingAsync(booking.Id, userId);
        
        // Act & Assert
        await Assert.ThrowsAsync<BookingAlreadyCancelledException>(() => bookingService.CancelBookingAsync(booking.Id, userId));
    }
    
    [Fact]
    public async Task ProcessingBooking_WhenBookingAlreadyCancelled_ShouldBookingAlreadyCancelled()
    {
        // Arrange
        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<BookingService>();
        var ev = await CreateEventAsync(scope, 10);
        var userId = Guid.Empty;
        var booking = await bookingService.CreateBookingAsync(ev,userId);
        await bookingService.CancelBookingAsync(booking.Id, userId);
        var time = booking.ProcessedAt;
        
        // Act 
        await bookingService.ProcessBookingAsync(booking.Id, TestContext.Current.CancellationToken);
        
        // Assert
        Assert.Equal(time, booking.ProcessedAt);
    }
    
    [Fact]
    public async Task CreateBooking_WhenEventAlreadyStarted_ShouldPastEventBookingException()
    {
        // Arrange
        using var scope = _provider.CreateScope();
        
        var bookingService = scope.ServiceProvider.GetRequiredService<BookingService>();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        var ev = await eventService.CreateAsync(new CreateEventDto
        {
            Title = "Test",
            StartAt = DateTime.Now.AddDays(-1),
            EndAt = DateTime.Now.AddDays(1),
            TotalSeats = 10
        });
        
        var userId = Guid.Empty;
        
        // Act & Assert
        await Assert.ThrowsAsync<PastEventBookingException>(() => bookingService.CreateBookingAsync(ev.Id, userId));
    }
    
    [Fact]
    public async Task CreateBooking_WhenSeatsLimitExceeded_ShouldThrowSeatsLimitExceededException()
    {
        // Arrange
        using var scope = _provider.CreateScope();
        
        var bookingService = scope.ServiceProvider.GetRequiredService<BookingService>();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var  ev  = await eventService.CreateAsync(new CreateEventDto
        {
            Title = "Test",
            StartAt = DateTime.Now.AddDays(1),
            EndAt = DateTime.Now.AddDays(2),
            TotalSeats = 12
        });
        
        var userId = Guid.Empty;
        var bookings = new List<Booking>();
        
        // Act
        foreach (var _ in Enumerable.Range(0, 10))
        {
            bookings.Add(await bookingService.CreateBookingAsync(ev.Id, userId));
        }
        
        var tasks = bookings.Select(b => 
            Task.Run(async () => await bookingService.ProcessBookingAsync(b.Id,  TestContext.Current.CancellationToken)));

        await Task.WhenAll(tasks);
        
        // Assert
        var exception = await Assert.ThrowsAsync<BusinessRuleViolationException>(() => bookingService.CreateBookingAsync(ev.Id, userId));
        Assert.Equal("No available seats", exception.RuleName);
    }
    
    [Fact]
    public async Task CreateBooking_WhenBookingDifferentUser_ShouldAllowBooking()
    {
        // Arrange
        using var scope = _provider.CreateScope();
        
        var bookingService = scope.ServiceProvider.GetRequiredService<BookingService>();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var  ev  = await eventService.CreateAsync(new CreateEventDto
        {
            Title = "Test",
            StartAt = DateTime.Now.AddDays(1),
            EndAt = DateTime.Now.AddDays(2),
            TotalSeats = 21
        });
        
        var fUserId = Guid.NewGuid();
        var sUserId = Guid.NewGuid();
        
        var bookings = new List<Booking>();
        
        // Act
        foreach (var _ in Enumerable.Range(0, 9))
        {
            bookings.Add(await bookingService.CreateBookingAsync(ev.Id, fUserId));
            bookings.Add(await bookingService.CreateBookingAsync(ev.Id, sUserId));
        }
        
        var tasks = bookings.Select(b => 
            Task.Run(async () => await bookingService.ProcessBookingAsync(b.Id,  TestContext.Current.CancellationToken)));

        await Task.WhenAll(tasks);
        
        var exceptionFUser = await Record.ExceptionAsync(
            () => bookingService.CreateBookingAsync(ev.Id, fUserId));
        
        var exceptionSUser = await Record.ExceptionAsync(
            () => bookingService.CreateBookingAsync(ev.Id, sUserId));
        
        // Assert
        Assert.Null(exceptionFUser);
        Assert.Null(exceptionSUser);
    }
}