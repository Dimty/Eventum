using Eventum.Models;
using Eventum.Services;

namespace Eventum.Tests;

public class EventServiceTests
{
    private readonly EventService _service = new();
    
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
    }

}