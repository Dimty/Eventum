using Eventum.Events.Domain;
using Eventum.Events.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Eventum.Events.IntegrationTests;

public class EventRepositoryIntegrationTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task AddAsync_ShouldPersistEvent()
    {
        var token = TestContext.Current.CancellationToken;
        await using var context = await CreateContextAsync(token);
        var repository = new EventRepository(context);
        var ev = CreateEvent("Conference", DateTime.UtcNow.AddDays(1));

        await repository.AddAsync(ev, token);
        await repository.SaveChangesAsync(token);

        context.ChangeTracker.Clear();
        var saved = await repository.GetByIdAsync(ev.Id, token);

        Assert.NotNull(saved);
        Assert.Equal("Conference", saved.Title);
        Assert.Equal(10, saved.AvailableSeats);
    }

    [Fact]
    public async Task GetAllAsync_ShouldPaginate()
    {
        var token = TestContext.Current.CancellationToken;
        await using var context = await CreateContextAsync(token);
        var repository = new EventRepository(context);
        await repository.AddAsync(CreateEvent("Conference", DateTime.UtcNow.AddDays(2)), token);
        await repository.AddAsync(CreateEvent("Meetup", DateTime.UtcNow.AddDays(3)), token);
        await repository.AddAsync(CreateEvent("Conference Workshop", DateTime.UtcNow.AddDays(4)), token);
        await repository.SaveChangesAsync(token);

        var result = await repository.GetAllAsync(page: 2, pageSize: 1, token: token);

        Assert.Equal(3, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Meetup", result.Items.First().Title);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByTitle_WithPostgreSqlILike()
    {
        var token = TestContext.Current.CancellationToken;
        await using var context = await CreateContextAsync(token);
        var repository = new EventRepository(context);
        await repository.AddAsync(CreateEvent("Conference", DateTime.UtcNow.AddDays(2)), token);
        await repository.AddAsync(CreateEvent("Meetup", DateTime.UtcNow.AddDays(3)), token);
        await repository.SaveChangesAsync(token);

        var result = await repository.GetAllAsync(title: "conf", token: token);

        Assert.Single(result.Items);
        Assert.Equal("Conference", result.Items.First().Title);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByDateRange()
    {
        var token = TestContext.Current.CancellationToken;
        await using var context = await CreateContextAsync(token);
        var repository = new EventRepository(context);
        var now = DateTime.UtcNow;

        await repository.AddAsync(CreateEvent("Past", now.AddDays(-2)), token);
        await repository.AddAsync(CreateEvent("Target", now.AddDays(2)), token);
        await repository.AddAsync(CreateEvent("Future", now.AddDays(10)), token);
        await repository.SaveChangesAsync(token);

        var result = await repository.GetAllAsync(from: now.AddDays(1), to: now.AddDays(3).AddHours(2), token: token);

        Assert.Single(result.Items);
        Assert.Equal("Target", result.Items.Single().Title);
    }

    [Fact]
    public async Task GetAllAsync_ShouldApplyAllFilters()
    {
        var token = TestContext.Current.CancellationToken;
        await using var context = await CreateContextAsync(token);
        var repository = new EventRepository(context);
        var now = DateTime.UtcNow;

        await repository.AddAsync(CreateEvent("Backend Meetup", now.AddDays(1)), token);
        await repository.AddAsync(CreateEvent("Frontend Meetup", now.AddDays(2)), token);
        await repository.AddAsync(CreateEvent("Architecture Workshop", now.AddDays(2)), token);
        await repository.SaveChangesAsync(token);

        var result = await repository.GetAllAsync(
            title: "meetup",
            from: now.AddHours(12),
            to: now.AddDays(2).AddHours(3),
            page: 1,
            pageSize: 10,
            token: token);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(["Backend Meetup", "Frontend Meetup"], result.Items.Select(item => item.Title).ToArray());
    }

    [Fact]
    public async Task Delete_ShouldRemoveEvent_AfterSaveChanges()
    {
        var token = TestContext.Current.CancellationToken;
        await using var context = await CreateContextAsync(token);
        var repository = new EventRepository(context);
        var ev = CreateEvent("Conference", DateTime.UtcNow.AddDays(1));
        await repository.AddAsync(ev, token);
        await repository.SaveChangesAsync(token);

        repository.Delete(ev);
        await repository.SaveChangesAsync(token);

        context.ChangeTracker.Clear();
        var saved = await repository.GetByIdAsync(ev.Id, token);

        Assert.Null(saved);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldPersistSeatDecrease()
    {
        var token = TestContext.Current.CancellationToken;
        await using var context = await CreateContextAsync(token);
        var repository = new EventRepository(context);
        var ev = CreateEvent("Conference", DateTime.UtcNow.AddDays(1));
        await repository.AddAsync(ev, token);
        await repository.SaveChangesAsync(token);

        ev.TryDecreaseAvailableSeats(2);
        await repository.SaveChangesAsync(token);

        context.ChangeTracker.Clear();
        var saved = await repository.GetByIdAsync(ev.Id, token);

        Assert.NotNull(saved);
        Assert.Equal(8, saved.AvailableSeats);
    }

    private static Event CreateEvent(string title, DateTime startAt) =>
        Event.Create(title, "Description", startAt, startAt.AddHours(2), 10);

    private async Task<EventsDbContext> CreateContextAsync(CancellationToken token)
    {
        var connectionString = new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
        {
            Database = $"eventum_events_{Guid.NewGuid():N}"
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<EventsDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        var context = new EventsDbContext(options);
        await context.Database.EnsureCreatedAsync(token);
        return context;
    }
}
