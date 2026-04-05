using Eventum.Exceptions;
using Eventum.Models;
using Eventum.Services;

namespace Eventum.Tests;

public class EventServiceTests
{
    private readonly EventService _service = new();

    private Event CreateInstance(string title, DateTime startAt, DateTime endAt) =>
        _service.Create(new Event
        {
            Title = title,
            StartAt = startAt,
            EndAt = endAt
        });

    [Fact]
    public void Create_ShouldAddEvent()
    {
        var ev = new Event
        {
            Title = "Test",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(1),
            Description = "Test description"
        };

        var result = _service.Create(ev);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(ev.Title, result.Title);
    }

    [Fact]
    public void GetAll_ShouldReturnAllEvents()
    {
        CreateInstance("EventA", DateTime.Now, DateTime.Now.AddHours(1));
        CreateInstance("EventB", DateTime.Now, DateTime.Now.AddHours(1));

        var result = _service.GetAll(null, null, null, 1, 10);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
    }
    
    [Fact]
    public void GetById_ShouldReturnEvent()
    {
        var ev = CreateInstance("EventA", DateTime.Now, DateTime.Now.AddHours(1));
        
        var result = _service.GetById(ev.Id);
        
        Assert.Equal(ev.Title, result!.Title);
    }
    
    [Fact]
    public void GetById_ShouldThrow_IfNotFound()
    {
        Assert.Throws<NotFoundException>(() =>
            _service.GetById(Guid.NewGuid()));
    }
    
    [Fact]
    public void Update_ShouldUpdateEvent()
    {
        var ev = CreateInstance("Old", DateTime.Now, DateTime.Now.AddHours(1));

        _service.Update(ev.Id, new Event
        {
            Title = "New",
            StartAt = ev.StartAt,
            EndAt = ev.EndAt
        });

        var updated = _service.GetById(ev.Id);

        Assert.Equal("New", updated!.Title);
    }
    
    [Fact]
    public void Update_ShouldThrow_IfNotFound()
    {
        Assert.Throws<NotFoundException>(() =>
            _service.Update(Guid.NewGuid(), new Event()));
    }
    
    [Fact]
    public void Delete_ShouldRemoveEvent()
    {
        var ev = CreateInstance("Event", DateTime.Now, DateTime.Now.AddHours(1));

        _service.Delete(ev.Id);

        Assert.Throws<NotFoundException>(() =>
            _service.GetById(ev.Id));
    }
    
    [Fact]
    public void Delete_ShouldThrow_IfNotFound()
    {
        Assert.Throws<NotFoundException>(() =>
            _service.Delete(Guid.NewGuid()));
    }

    [Fact]
    public void GetAll_ShouldFilterByTitle()
    {
        CreateInstance("Working", DateTime.Now, DateTime.Now.AddHours(1));
        CreateInstance("Party", DateTime.Now, DateTime.Now.AddHours(1));

        var result = _service.GetAll("work", null, null, 1, 10);

        Assert.Single(result.Items);
    }
    
    [Fact]
    public void GetAll_ShouldFilterByDateRange()
    {
        var now = DateTime.Now;

        CreateInstance("Working", now.AddDays(-2), now.AddDays(-1));
        CreateInstance("Party", now.AddDays(1), now.AddDays(2));

        var result = _service.GetAll(null, now, now.AddDays(3), 1, 10);

        Assert.Single(result.Items);
        Assert.Equal("Party", result.Items.First().Title);
    }
    
    [Fact]
    public void GetAll_ShouldPaginate()
    {
        CreateInstance("Working", DateTime.Now, DateTime.Now.AddHours(1));
        CreateInstance("Party", DateTime.Now, DateTime.Now.AddHours(1));
        CreateInstance("Relax", DateTime.Now, DateTime.Now.AddHours(1));
        CreateInstance("Relax one more time", DateTime.Now, DateTime.Now.AddHours(1));

        var result = _service.GetAll(null, null, null, 2, 2);

        Assert.Equal(4, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal("Relax", result.Items.First().Title);
    }
    
    [Fact]
    public void GetAll_ShouldApplyAllFilters()
    {
        var now = DateTime.Now;

        CreateInstance("Working", now, now.AddHours(1));
        CreateInstance("Party", now.AddDays(5), now.AddDays(6));
        CreateInstance("Relax on office", now.AddDays(1), now.AddHours(1));
        CreateInstance("Relax one more time", now, now.AddMonths(1));

        var result = _service.GetAll(
            title: "relax",
            from: now,
            to: now.AddDays(10),
            page: 1,
            pageSize: 10);

        Assert.Single(result.Items);
        Assert.Equal("Relax on office", result.Items.First().Title);
    }
}