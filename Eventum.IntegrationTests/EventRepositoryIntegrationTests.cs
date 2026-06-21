using Eventum.IntegrationTests.Base;
using Eventum.IntegrationTests.Fixtures;
using Eventum.Domain.Models;
using Eventum.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Eventum.IntegrationTests;

[Collection("Database collection")]
public class EventRepositoryIntegrationTests(DatabaseCollectionFixture fixture) : DatabaseTestBase(fixture)
{
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
        // Arrange
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

        // Act
        await repo.AddAsync(newEvent, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var ev = await repo.GetAllAsync(token: TestContext.Current.CancellationToken);

        Assert.NotNull(ev);
        Assert.NotEqual(Guid.Empty, ev.Items.First().Id);
        Assert.Equal(newEvent.Title, ev.Items.First().Title);
        Assert.Equal(newEvent.Description, ev.Items.First().Description);
        Assert.Equal(newEvent.TotalSeats, ev.Items.First().TotalSeats);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllEvents_WhenNoFiltersApplied()
    {
        // Arrange
        await ResetDatabaseAsync();
        await SeedTestDataAsync();

        var repo = new EventRepository(CreateContext());

        // Act
        var result = await repo.GetAllAsync(token: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(5, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByTitle_WhenTitleProvided()
    {
        // Arrange
        await ResetDatabaseAsync();
        await SeedTestDataAsync();

        var repo = new EventRepository(CreateContext());

        // Act
        var result = await repo.GetAllAsync("Workshop", token: TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Workshop", result.Items.First().Title);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByDateRange()
    {
        // Arrange
        await ResetDatabaseAsync();
        await SeedTestDataAsync();

        var from = DateTime.UtcNow.AddDays(8);
        var to = DateTime.UtcNow.AddDays(22);
        var repo = new EventRepository(CreateContext());

        // Act
        var result = await repo.GetAllAsync(null, from, to, token: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, result.Items.Count());
        Assert.All(result.Items, e =>
            Assert.True(e.StartAt >= from && e.StartAt <= to));
    }

    [Fact]
    public async Task GetAllAsync_ShouldApplyPagination()
    {
        // Arrange
        await ResetDatabaseAsync();
        await SeedTestDataAsync();

        var repo = new EventRepository(CreateContext());

        // Act
        var page1 = await repo.GetAllAsync(page: 1, pageSize: 2, token: TestContext.Current.CancellationToken);
        var page2 = await repo.GetAllAsync(page: 2, pageSize: 2, token: TestContext.Current.CancellationToken);
        var page3 = await repo.GetAllAsync(page: 3, pageSize: 2, token: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(2, page1.Items.Count());
        Assert.Equal(2, page2.Items.Count());
        Assert.Single(page3.Items);
    }

    [Fact]
    public async Task GetAllAsync_ShouldCombineFilters()
    {
        // Arrange
        await ResetDatabaseAsync();
        await SeedTestDataAsync();

        var repo = new EventRepository(CreateContext());

        // Act
        var result = await repo.GetAllAsync("o", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(25),
            token: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Items.Count());
        Assert.All(result.Items, e =>
            Assert.Contains("o", e.Title, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnEvent_WhenEventExists()
    {
        // Arrange
        await ResetDatabaseAsync();
        await SeedTestDataAsync();

        var context = CreateContext();
        var repo = new EventRepository(context);
        var existingEvent = await context.Events.FirstAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var result = await repo.GetByIdAsync(existingEvent.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingEvent.Id, result.Id);
        Assert.Equal(existingEvent.Title, result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenEventNotExists()
    {
        // Arrange
        await ResetDatabaseAsync();
        var nonExistentId = Guid.NewGuid();

        var context = CreateContext();
        var repo = new EventRepository(context);

        // Act
        var ev = await repo.GetByIdAsync(nonExistentId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(ev);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteEvent_WhenEventExists()
    {
        // Arrange
        await ResetDatabaseAsync();
        await SeedTestDataAsync();
        var context = CreateContext();
        var repo = new EventRepository(context);
        var existingEvent = await context.Events.FirstAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Act
        await repo.DeleteAsync(existingEvent, TestContext.Current.CancellationToken);
        await repo.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var newContext = CreateContext();
        var deletedEvent = await newContext.Events.FindAsync([existingEvent.Id], TestContext.Current.CancellationToken);
        Assert.Null(deletedEvent);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDoNothing_WhenEventNotExists()
    {
        // Arrange
        await ResetDatabaseAsync();
        var nonExistentEvent = Event.Create("Hackathon", "Coding competition",
            DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(30).AddHours(24), 150);
        var context = CreateContext();
        var repo = new EventRepository(context);

        // Act
        await repo.DeleteAsync(nonExistentEvent, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(true);
    }

    [Fact]
    public async Task UpdateEventAsync_ShouldUpdateExistingEvent_SaveChangesAsync()
    {
        // Arrange
        await ResetDatabaseAsync();
        await SeedTestDataAsync();

        var context = CreateContext();
        var repo = new EventRepository(context);
        var existingEvent = await context.Events
            .FirstAsync(cancellationToken: TestContext.Current.CancellationToken);

        existingEvent.Update("New Title", "New Description", DateTime.UtcNow.AddDays(30),
            DateTime.UtcNow.AddDays(30).AddHours(24));
        
        // Act
        await repo.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var updatedEvent = await new EventRepository(CreateContext())
            .GetByIdAsync(existingEvent.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(updatedEvent);
        Assert.Equal("New Title", updatedEvent.Title);
        Assert.Equal("New Description", updatedEvent.Description);
    }
    
}