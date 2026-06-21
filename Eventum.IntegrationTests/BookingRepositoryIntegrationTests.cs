using Eventum.IntegrationTests.Base;
using Eventum.IntegrationTests.Fixtures;
using Eventum.Domain.Models;
using Eventum.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Eventum.IntegrationTests;

[Collection("Database collection")]
public class BookingRepositoryIntegrationTests(DatabaseCollectionFixture fixture) : DatabaseTestBase(fixture)
{
    
    private async Task SeedTestDataAsync()
    {
        var context = CreateContext();
        var events = new List<Event>
        {
            Event.Create("Conference 2024", "Tech conference", 
                DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(10).AddHours(8), 100),
            Event.Create("Workshop", "Programming workshop", 
                DateTime.UtcNow.AddDays(20), DateTime.UtcNow.AddDays(20).AddHours(4), 50)
        };

        await context.Events.AddRangeAsync(events);
        await context.SaveChangesAsync();

        var bookings = new List<Booking>
        {
            new (events[0].Id),
            new (events[0].Id),
            new (events[1].Id),
            new (events[1].Id)
        };

        await context.Bookings.AddRangeAsync(bookings);
        await context.SaveChangesAsync();
    }
    
    [Fact]
    public async Task FindWithProjectionAsync_ShouldReturnAllBookings_WhenNoFilter()
    {
        // Arrange
        await ResetDatabaseAsync();
        await SeedTestDataAsync();
        
        var context = CreateContext();
        var repo = new BookingRepository(context);
        
        // Act
        var result = await repo.FindWithProjectionAsync(b => true, 
            b => b.Id, TestContext.Current.CancellationToken);

        // Assert
        var enumerable = result.ToList();
        Assert.Equal(4, enumerable.Count());
        Assert.All(enumerable, booking => 
        {
            Assert.NotEqual(Guid.Empty, booking);
        });
    }
    
    [Fact]
    public async Task FindWithProjectionAsync_ShouldFilterByEventId()
    {
        // Arrange
        await ResetDatabaseAsync();
        await SeedTestDataAsync();
        
        var context = CreateContext();
        var repo = new BookingRepository(context);
        var targetEvent = await context.Events.FirstAsync(e => e.Title == "Conference 2024", cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var result = await repo.FindWithProjectionAsync(b => b.EventId == targetEvent.Id, 
            b => new { b.Id }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Count());
    }
    
    [Fact]
    public async Task FindWithProjectionAsync_ShouldReturnEmpty_WhenNoMatches()
    {
        // Arrange
        await ResetDatabaseAsync();
        await SeedTestDataAsync();

        var context = CreateContext();
        var repo = new BookingRepository(context);
        
        // Act
        var result = await repo.FindWithProjectionAsync(b => b.Id == Guid.NewGuid(), 
            b => new { b.Id }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
    }
    
    [Fact]
    public async Task GetByIdAsync_ShouldReturnBooking_WhenExists()
    {
        // Arrange
        await ResetDatabaseAsync();
        await SeedTestDataAsync();
       
        var context = CreateContext();
        var repo = new BookingRepository(context);
        var existingBooking = await context.Bookings.FirstAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var result = await repo.GetByIdAsync(existingBooking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingBooking.Id, result.Id);
    }   
    
    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        await ResetDatabaseAsync();
        
        var nonExistentId = Guid.NewGuid();
        var context = CreateContext();
        var repo = new BookingRepository(context);
        
        // Act
        var result = await repo.GetByIdAsync(nonExistentId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public async Task GetByIdAsync_ShouldIncludeEventDetails_WhenBookingExists()
    {
        // Arrange
        await ResetDatabaseAsync();
        await SeedTestDataAsync();
        
        var context = CreateContext();
        var repo = new BookingRepository(context);
        var existingBooking = await context.Bookings.FirstAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var result = await repo.GetByIdAsync(existingBooking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.EventId);
    }
    
    [Fact]
    public async Task AddAsync_ShouldAddBooking_WhenValidData()
    {
        // Arrange
        await ResetDatabaseAsync();
        await SeedTestDataAsync();
        var context = CreateContext();
        var repo = new BookingRepository(context);
        var existingEvent = await context.Events.FirstAsync(cancellationToken: TestContext.Current.CancellationToken);
        var newBooking = new Booking(existingEvent.Id);

        // Act
        await repo.AddAsync(newBooking, TestContext.Current.CancellationToken);
        await repo.SaveChangesAsync(TestContext.Current.CancellationToken);
        
        // Assert
        var newContext = CreateContext();
        var savedBooking = await newContext.Bookings.FindAsync([newBooking.Id], TestContext.Current.CancellationToken);
        Assert.NotNull(savedBooking);
        Assert.Equal(existingEvent.Id, savedBooking.EventId);
    }
    
    [Fact]
    public async Task AddAsync_ShouldHandleMultipleBookings()
    {
        // Arrange
        await ResetDatabaseAsync();
        await SeedTestDataAsync();
        
        var context = CreateContext();
        var repo = new BookingRepository(context);
        var existingEvent = await context.Events.FirstAsync(cancellationToken: TestContext.Current.CancellationToken);
        var bookings = new Booking[]
        {
            new(existingEvent.Id),
            new(existingEvent.Id),
            new(existingEvent.Id)
        };

        // Act
        foreach (var booking in bookings)
        {
            await repo.AddAsync(booking, TestContext.Current.CancellationToken);
        }
        await repo.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var newContext = CreateContext();
        var totalBookings = await newContext.Bookings.CountAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(7, totalBookings);
    }
    
    [Fact]
    public async Task AddAsync_ShouldNotPersist_WithoutSaveChanges()
    {
        // Arrange
        await ResetDatabaseAsync();
        await SeedTestDataAsync();
        
        var context = CreateContext();
        var repo = new BookingRepository(context);
        var existingEvent = await context.Events.FirstAsync(cancellationToken: TestContext.Current.CancellationToken);
        var newBooking = new Booking(existingEvent.Id);

        // Act
        await repo.AddAsync(newBooking, TestContext.Current.CancellationToken);

        // Assert
        var newContext = CreateContext();
        var bookingInDb = await newContext.Bookings.FindAsync([newBooking.Id], TestContext.Current.CancellationToken);
        Assert.Null(bookingInDb);
    }
    
    [Fact]
    public async Task SaveChangesAsync_ShouldPersistAllChanges()
    {
        // Arrange
        await ResetDatabaseAsync();
        await SeedTestDataAsync();
        
        var context = CreateContext();
        var repo = new BookingRepository(context);
        var existingEvent = await context.Events.FirstAsync(cancellationToken: TestContext.Current.CancellationToken);
        var newBooking = new Booking(existingEvent.Id);

        // Act
        await repo.AddAsync(newBooking, TestContext.Current.CancellationToken);
        await repo.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var newContext = CreateContext();
        var savedBooking = await newContext.Bookings.FindAsync([newBooking.Id], TestContext.Current.CancellationToken);
        Assert.NotNull(savedBooking);
    }
    
    [Fact]
    public async Task SaveChangesAsync_ShouldHandleMultipleOperations()
    {
        // Arrange
        await ResetDatabaseAsync();
        await SeedTestDataAsync();
        
        var context = CreateContext();
        var repo = new BookingRepository(context);
        var existingEvent = await context.Events.FirstAsync(cancellationToken: TestContext.Current.CancellationToken);
        
        var booking1 = new Booking(existingEvent.Id);
        var booking2 = new Booking(existingEvent.Id);
        
        // Act
        await repo.AddAsync(booking1, TestContext.Current.CancellationToken);
        await repo.AddAsync(booking2, TestContext.Current.CancellationToken);
        await repo.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var newContext = CreateContext();
        var allBookings = await newContext.Bookings.CountAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(6, allBookings); 
    }
    
    [Fact]
    public async Task FullLifecycle_ShouldWorkCorrectly()
    {
        // Arrange
        await ResetDatabaseAsync();
        await SeedTestDataAsync();
        
        var context = CreateContext();
        var repo = new BookingRepository(context);
        var existingEvent = await context.Events.FirstAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
        {
            var newBooking = new Booking(existingEvent.Id);
            await repo.AddAsync(newBooking, TestContext.Current.CancellationToken);
            await repo.SaveChangesAsync(TestContext.Current.CancellationToken);

            var retrieved = await repo.GetByIdAsync(newBooking.Id, TestContext.Current.CancellationToken);
            Assert.NotNull(retrieved);

            var projected = await repo.FindWithProjectionAsync(b => b.Id == newBooking.Id, b => new { b.Id }
                , TestContext.Current.CancellationToken);
            Assert.Single(projected);
        });

        Assert.Null(exception);
    }
    
    [Fact]
    public async Task AddAsync_ShouldAssignNewId_ForEachBooking()
    {
        // Arrange
        await ResetDatabaseAsync();
        await SeedTestDataAsync();
        
        var context = CreateContext();
        var repo = new BookingRepository(context);
        var existingEvent = await context.Events.FirstAsync(cancellationToken: TestContext.Current.CancellationToken);
        var booking1 = new Booking(existingEvent.Id);
        var booking2 = new Booking(existingEvent.Id);

        // Act
        await repo.AddAsync(booking1, TestContext.Current.CancellationToken);
        await repo.AddAsync(booking2, TestContext.Current.CancellationToken);
        await repo.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEqual(Guid.Empty, booking1.Id);
        Assert.NotEqual(Guid.Empty, booking2.Id);
        Assert.NotEqual(booking1.Id, booking2.Id);
    }
}