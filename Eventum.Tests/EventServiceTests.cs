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
        
        Assert.Equal(ev.Title, result.Title);
    }
}