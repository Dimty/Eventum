using Eventum.Infrastructure.Data.Contexts;
using Eventum.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Eventum.IntegrationTests.Base;


public abstract class DatabaseTestBase(DatabaseCollectionFixture fixture) : IAsyncLifetime
{
    protected AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public async Task ResetDatabaseAsync()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE events, bookings RESTART IDENTITY CASCADE");
    }

    public async ValueTask InitializeAsync() => await Task.CompletedTask;

    public async ValueTask DisposeAsync() => await Task.CompletedTask;
}