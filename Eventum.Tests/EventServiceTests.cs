using Eventum.DataAccess.Contexts;
using Eventum.DTO;
using Eventum.Exceptions;
using Eventum.Models;
using Eventum.Services;
using Eventum.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Eventum.Tests;

public class EventServiceTests
{
    private readonly ServiceProvider _provider;

    public EventServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();

        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddScoped<IEventService, EventService>();

        _provider = services.BuildServiceProvider();
    }

    private async Task<Event> CreateInstanceAsync(
        IEventService service,
        string title,
        DateTime startAt,
        DateTime endAt,
        int totalSeats = 3) => await service.CreateAsync(new CreateEventDto
    {
        Title = title,
        StartAt = startAt,
        EndAt = endAt,
        TotalSeats = totalSeats
    });


    [Fact]
    public async Task Create_ShouldAddEvent()
    {
        using var scope = _provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        var ev = new CreateEventDto
        {
            Title = "Test",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(1),
            Description = "Test description",
            TotalSeats = 3
        };

        var result = await service.CreateAsync(ev);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(ev.Title, result.Title);
    }

    [Fact]
    public async Task GetAll_ShouldReturnAllEvents()
    {
        using var scope = _provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        await CreateInstanceAsync(
            service,
            "EventA",
            DateTime.Now,
            DateTime.Now.AddHours(1));

        await CreateInstanceAsync(
            service,
            "EventB",
            DateTime.Now,
            DateTime.Now.AddHours(1));

        var result = await service.GetAllAsync(null, null, null);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public async Task GetById_ShouldReturnEvent()
    {
        using var scope = _provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        var ev = await CreateInstanceAsync(
            service,
            "EventA",
            DateTime.Now,
            DateTime.Now.AddHours(1));

        var result = await service.GetByIdAsync(ev.Id);

        Assert.Equal(ev.Title, result!.Title);
    }

    [Fact]
    public async Task GetById_ShouldThrow_IfNotFound()
    {
        using var scope = _provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        await Assert.ThrowsAsync<NotFoundException>(async () =>
            await service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Update_ShouldUpdateEvent()
    {
        using var scope = _provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        var ev = await CreateInstanceAsync(
            service,
            "Old",
            DateTime.Now,
            DateTime.Now.AddHours(1));

        await service.UpdateAsync(ev.Id, new UpdateEventDto
        {
            Title = "New",
            StartAt = ev.StartAt,
            EndAt = ev.EndAt
        });

        var updated = await service.GetByIdAsync(ev.Id);

        Assert.Equal("New", updated!.Title);
    }

    [Fact]
    public async Task Update_ShouldThrow_IfNotFound()
    {
        using var scope = _provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.UpdateAsync(Guid.NewGuid(), new UpdateEventDto()));
    }

    [Fact]
    public async Task Delete_ShouldRemoveEvent()
    {
        using var scope = _provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        var ev = await CreateInstanceAsync(
            service,
            "Old",
            DateTime.Now,
            DateTime.Now.AddHours(1));

        await service.DeleteAsync(ev.Id);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetByIdAsync(ev.Id));
    }

    [Fact]
    public async Task Delete_ShouldThrow_IfNotFound()
    {
        using var scope = _provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetAll_ShouldFilterByTitle()
    {
        using var scope = _provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        await CreateInstanceAsync(
            service,
            "work",
            DateTime.Now,
            DateTime.Now.AddHours(1));

        var result = await service.GetAllAsync("work", null, null);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetAll_ShouldFilterByDateRange()
    {
        using var scope = _provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        var now = DateTime.Now;

        await CreateInstanceAsync(
            service,
            "Working",
            now.AddDays(-2),
            now.AddHours(-1));

        await CreateInstanceAsync(
            service,
            "Party",
            now.AddDays(1),
            now.AddDays(2));

        var result = await service.GetAllAsync(null, now, now.AddDays(3));

        Assert.Single(result.Items);
        Assert.Equal("Party", result.Items.First().Title);
    }

    [Fact]
    public async Task GetAll_ShouldPaginate()
    {
        using var scope = _provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        var now = DateTime.Now;

        await CreateInstanceAsync(
            service,
            "Working",
            now,
            now.AddHours(1));

        await CreateInstanceAsync(
            service,
            "Party",
            now,
            now.AddHours(2));

        await CreateInstanceAsync(
            service,
            "Relax",
            now,
            now.AddHours(1));

        await CreateInstanceAsync(
            service,
            "Relax one more time",
            now,
            now.AddHours(1));

        var result = await service.GetAllAsync(null, null, null, 2, 2);

        Assert.Equal(4, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal("Relax", result.Items.First().Title);
    }

    [Fact]
    public async Task GetAll_ShouldApplyAllFilters()
    {
        using var scope = _provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        var now = DateTime.Now;

        await CreateInstanceAsync(
            service,
            "Working",
            now,
            now.AddHours(1));

        await CreateInstanceAsync(
            service,
            "Party",
            now.AddDays(5),
            now.AddDays(6));

        await CreateInstanceAsync(
            service,
            "Relax on office",
            now.AddDays(1),
            now.AddDays(1).AddHours(1));

        await CreateInstanceAsync(
            service,
            "Relax one more time",
            now,
            now.AddMonths(1));


        var result = await service.GetAllAsync(
            title: "relax",
            from: now,
            to: now.AddDays(10),
            page: 1,
            pageSize: 10);

        Assert.Single(result.Items);
        Assert.Equal("Relax on office", result.Items.First().Title);
    }
}