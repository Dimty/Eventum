using Eventum.Data.Repositories;
using Eventum.DataAccess.Contexts;
using Eventum.Models;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Eventum.IntegrationTests;

public class EventServiceIntegrationTests : IAsyncLifetime
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
                DateTime.UtcNow.AddDays(20), DateTime.UtcNow.AddDays(20).AddHours(4), 50),
            Event.Create("Meetup", "Local meetup", 
                DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(5).AddHours(3), 30),
            Event.Create("Webinar", "Online webinar", 
                DateTime.UtcNow.AddDays(15), DateTime.UtcNow.AddDays(15).AddHours(2), 200),
            Event.Create("Hackathon", "Coding competition", 
                DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(30).AddHours(24), 150)
        };

        await context.Events.AddRangeAsync(events);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateEvent()
    {
        await ResetDatabaseAsync();
        var context = CreateContext();
        var repo = new EventRepository(context);
        var newEvent = Event.Create(
            "New Event",
            "Test Description",
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(7).AddHours(4),
            50
        );

        
        await repo.AddAsync(newEvent, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        
        var ev = await repo.GetAllAsync(token: TestContext.Current.CancellationToken);
        
        Assert.NotNull(ev);
        Assert.NotEqual(Guid.Empty, ev.Items.First().Id);
        Assert.Equal(newEvent.Title, ev.Items.First().Title);
        Assert.Equal(newEvent.Description, ev.Items.First().Description);
        Assert.Equal(newEvent.TotalSeats, ev.Items.First().TotalSeats);
    }
    
}