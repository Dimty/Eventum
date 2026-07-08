using System.ComponentModel.DataAnnotations;
using Eventum.Events.Application.DTO;
using Eventum.Events.Application.Exceptions;
using Eventum.Events.Application.Interfaces;
using Eventum.Events.Application.Services;
using Eventum.Events.Domain;

namespace Eventum.Events.Tests;

public class EventServiceTests
{
    [Fact]
    public void Create_ShouldThrow_WhenEndAtIsBeforeStartAt()
    {
        Assert.Throws<ValidationException>(() =>
            Event.Create("Event", null, DateTime.UtcNow.AddHours(2), DateTime.UtcNow, 10));
    }

    [Fact]
    public void Create_ShouldThrow_WhenTotalSeatsIsNotPositive()
    {
        Assert.Throws<ValidationException>(() =>
            Event.Create("Event", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 0));
    }

    [Fact]
    public async Task CreateAsync_ShouldAddEvent()
    {
        var token = TestContext.Current.CancellationToken;
        var repository = new InMemoryEventRepository();
        var service = new EventService(repository);

        var result = await service.CreateAsync(new CreateEventDto
        {
            Title = "Conference",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(1).AddHours(2),
            TotalSeats = 10
        }, token);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(10, result.AvailableSeats);
        Assert.Single(repository.Events);
    }

    [Fact]
    public async Task GetAllAsync_ShouldApplyFiltersAndPagination()
    {
        var token = TestContext.Current.CancellationToken;
        var repository = new InMemoryEventRepository();
        var service = new EventService(repository);
        var now = DateTime.UtcNow;

        await service.CreateAsync(new CreateEventDto
        {
            Title = "Backend Meetup",
            StartAt = now.AddDays(1),
            EndAt = now.AddDays(1).AddHours(2),
            TotalSeats = 20
        }, token);
        await service.CreateAsync(new CreateEventDto
        {
            Title = "Frontend Meetup",
            StartAt = now.AddDays(2),
            EndAt = now.AddDays(2).AddHours(2),
            TotalSeats = 20
        }, token);
        await service.CreateAsync(new CreateEventDto
        {
            Title = "Architecture Workshop",
            StartAt = now.AddDays(3),
            EndAt = now.AddDays(3).AddHours(2),
            TotalSeats = 20
        }, token);

        var result = await service.GetAllAsync(
            title: "meetup",
            from: now.AddHours(12),
            to: now.AddDays(2).AddHours(3),
            page: 2,
            pageSize: 1,
            token: token);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(1, result.PageSize);
        Assert.Single(result.Items);
        Assert.Equal("Frontend Meetup", result.Items.Single().Title);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrow_WhenEventIsMissing()
    {
        var token = TestContext.Current.CancellationToken;
        var service = new EventService(new InMemoryEventRepository());

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.GetByIdAsync(Guid.NewGuid(), token));
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistChanges()
    {
        var token = TestContext.Current.CancellationToken;
        var repository = new InMemoryEventRepository();
        var service = new EventService(repository);
        var created = await service.CreateAsync(new CreateEventDto
        {
            Title = "Conference",
            Description = "Initial description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(1).AddHours(2),
            TotalSeats = 10
        }, token);
        var originalStartAt = created.StartAt;
        var originalEndAt = created.EndAt;

        await service.UpdateAsync(created.Id, new UpdateEventDto
        {
            Title = "Updated Conference",
            Description = "Updated description",
            StartAt = originalStartAt.AddHours(1),
            EndAt = originalEndAt.AddHours(1)
        }, token);

        var updated = await service.GetByIdAsync(created.Id, token);

        Assert.Equal("Updated Conference", updated.Title);
        Assert.Equal("Updated description", updated.Description);
        Assert.Equal(originalStartAt.AddHours(1), updated.StartAt);
        Assert.Equal(originalEndAt.AddHours(1), updated.EndAt);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveEvent()
    {
        var token = TestContext.Current.CancellationToken;
        var repository = new InMemoryEventRepository();
        var service = new EventService(repository);
        var created = await service.CreateAsync(new CreateEventDto
        {
            Title = "Conference",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(1).AddHours(2),
            TotalSeats = 10
        }, token);

        await service.DeleteAsync(created.Id, token);

        Assert.Empty(repository.Events);
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.GetByIdAsync(created.Id, token));
    }

    [Fact]
    public async Task ApplyBookingConfirmedAsync_ShouldDecreaseAvailableSeats()
    {
        var token = TestContext.Current.CancellationToken;
        var repository = new InMemoryEventRepository();
        var service = new EventService(repository);
        var ev = await service.CreateAsync(new CreateEventDto
        {
            Title = "Conference",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(1).AddHours(2),
            TotalSeats = 10
        }, token);

        var applied = await service.ApplyBookingConfirmedAsync(ev.Id, 2, token);

        Assert.True(applied);
        Assert.Equal(8, ev.AvailableSeats);
    }

    [Fact]
    public async Task ApplyBookingConfirmedAsync_ShouldReturnFalse_WhenNoSeatsAvailable()
    {
        var token = TestContext.Current.CancellationToken;
        var repository = new InMemoryEventRepository();
        var service = new EventService(repository);
        var ev = await service.CreateAsync(new CreateEventDto
        {
            Title = "Conference",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(1).AddHours(2),
            TotalSeats = 1
        }, token);

        var applied = await service.ApplyBookingConfirmedAsync(ev.Id, 2, token);

        Assert.False(applied);
        Assert.Equal(1, ev.AvailableSeats);
    }

    [Fact]
    public async Task ApplyBookingConfirmedAsync_ShouldReturnFalse_WhenEventIsMissing()
    {
        var token = TestContext.Current.CancellationToken;
        var service = new EventService(new InMemoryEventRepository());

        var applied = await service.ApplyBookingConfirmedAsync(Guid.NewGuid(), 1, token);

        Assert.False(applied);
    }

    private sealed class InMemoryEventRepository : IEventRepository
    {
        public List<Event> Events { get; } = [];

        public Task<PaginatedResult<Event>> GetAllAsync(
            string? title = null,
            DateTime? from = null,
            DateTime? to = null,
            int page = 1,
            int pageSize = 10,
            CancellationToken token = default)
        {
            IEnumerable<Event> query = Events;

            if (!string.IsNullOrWhiteSpace(title))
                query = query.Where(ev => ev.Title.Contains(title, StringComparison.OrdinalIgnoreCase));

            if (from.HasValue)
                query = query.Where(ev => ev.StartAt >= from.Value);

            if (to.HasValue)
                query = query.Where(ev => ev.EndAt <= to.Value);

            var items = query.OrderBy(ev => ev.StartAt).Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return Task.FromResult(new PaginatedResult<Event>
            {
                TotalCount = query.Count(),
                Page = page,
                PageSize = pageSize,
                Count = items.Count,
                Items = items
            });
        }

        public Task<Event?> GetByIdAsync(Guid id, CancellationToken token = default) =>
            Task.FromResult(Events.FirstOrDefault(ev => ev.Id == id));

        public Task AddAsync(Event ev, CancellationToken token = default)
        {
            Events.Add(ev);
            return Task.CompletedTask;
        }

        public void Delete(Event ev) => Events.Remove(ev);

        public Task SaveChangesAsync(CancellationToken token = default) => Task.CompletedTask;
    }
}
