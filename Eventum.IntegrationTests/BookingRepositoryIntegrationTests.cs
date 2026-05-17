using Eventum.Data.Repositories;
using Eventum.DataAccess.Contexts;
using Eventum.Models;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Eventum.IntegrationTests;

public class BookingRepositoryIntegrationTests: IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE events, bookings RESTART IDENTITY CASCADE");
    }
    
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
        await ResetDatabaseAsync();
        await SeedTestDataAsync();
        
        var context = CreateContext();
        var repo = new BookingRepository(context);
        
        var result = await repo.FindWithProjectionAsync(b => true, b => b.Id 
            , TestContext.Current.CancellationToken);

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
        await ResetDatabaseAsync();
        await SeedTestDataAsync();
        
        var context = CreateContext();
        var repo = new BookingRepository(context);
        var targetEvent = await context.Events.FirstAsync(e => e.Title == "Conference 2024", cancellationToken: TestContext.Current.CancellationToken);

        var result = await repo.FindWithProjectionAsync(b => b.EventId == targetEvent.Id, b => new { b.Id }
            , TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count());
    }
    
    [Fact]
    public async Task FindWithProjectionAsync_ShouldReturnEmpty_WhenNoMatches()
    {
        await ResetDatabaseAsync();
        await SeedTestDataAsync();

        var context = CreateContext();
        var repo = new BookingRepository(context);
        
        var result = await repo.FindWithProjectionAsync(b => b.Id == Guid.NewGuid(), b => new { b.Id }
            , TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }
    
    [Fact]
    public async Task GetByIdAsync_ShouldReturnBooking_WhenExists()
    {
        await ResetDatabaseAsync();
        await SeedTestDataAsync();
       
        var context = CreateContext();
        var repo = new BookingRepository(context);
        var existingBooking = await context.Bookings.FirstAsync(cancellationToken: TestContext.Current.CancellationToken);

        var result = await repo.GetByIdAsync(existingBooking.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(existingBooking.Id, result.Id);
    }   
    
    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        await ResetDatabaseAsync();
        
        var nonExistentId = Guid.NewGuid();
        var context = CreateContext();
        var repo = new BookingRepository(context);
        
        var result = await repo.GetByIdAsync(nonExistentId, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }
    
    [Fact]
    public async Task GetByIdAsync_ShouldIncludeEventDetails_WhenBookingExists()
    {
        await ResetDatabaseAsync();
        await SeedTestDataAsync();
        
        var context = CreateContext();
        var repo = new BookingRepository(context);
        var existingBooking = await context.Bookings.FirstAsync(cancellationToken: TestContext.Current.CancellationToken);

        var result = await repo.GetByIdAsync(existingBooking.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.EventId);
    }
    
    [Fact]
    public async Task AddAsync_ShouldAddBooking_WhenValidData()
    {
        await ResetDatabaseAsync();
        await SeedTestDataAsync();
        var context = CreateContext();
        var repo = new BookingRepository(context);
        var existingEvent = await context.Events.FirstAsync(cancellationToken: TestContext.Current.CancellationToken);
        var newBooking = new Booking(existingEvent.Id);

        await repo.AddAsync(newBooking, TestContext.Current.CancellationToken);
        await repo.SaveChangesAsync(TestContext.Current.CancellationToken);
        
        var newContext = CreateContext();
        var savedBooking = await newContext.Bookings.FindAsync([newBooking.Id], TestContext.Current.CancellationToken);
        Assert.NotNull(savedBooking);
        Assert.Equal(existingEvent.Id, savedBooking.EventId);
    }
    
    [Fact]
    public async Task AddAsync_ShouldHandleMultipleBookings()
    {
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

        foreach (var booking in bookings)
        {
            await repo.AddAsync(booking, TestContext.Current.CancellationToken);
        }
        await repo.SaveChangesAsync(TestContext.Current.CancellationToken);

        var newContext = CreateContext();
        var totalBookings = await newContext.Bookings.CountAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(7, totalBookings);
    }
    
    [Fact]
    public async Task AddAsync_ShouldNotPersist_WithoutSaveChanges()
    {
        await ResetDatabaseAsync();
        await SeedTestDataAsync();
        
        var context = CreateContext();
        var repo = new BookingRepository(context);
        var existingEvent = await context.Events.FirstAsync(cancellationToken: TestContext.Current.CancellationToken);
        var newBooking = new Booking(existingEvent.Id);

        await repo.AddAsync(newBooking, TestContext.Current.CancellationToken);

        var newContext = CreateContext();
        var bookingInDb = await newContext.Bookings.FindAsync([newBooking.Id], TestContext.Current.CancellationToken);
        Assert.Null(bookingInDb);
    }
}