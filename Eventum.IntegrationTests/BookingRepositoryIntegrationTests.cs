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
   
}