using Eventum.Models;
using Eventum.Services.Interfaces;

namespace Eventum.Services;

public class EventService: IEventService
{
    private readonly List<Event> _events = new();
    
    public IEnumerable<Event> GetAll()
    {
        return _events.AsReadOnly();
    }

    public Event? GetById(Guid id)
    {
        return _events.FirstOrDefault(x => x.Id == id);
    }

    public Event Create(Event newEvent)
    {
        newEvent.Id = Guid.NewGuid();
        _events.Add(newEvent);
        return newEvent;
    }

    public bool Update(Guid id, Event updatedEvent)
    {
        var ev = GetById(id);

        if (ev is null) return false;
        
        ev.Description = updatedEvent.Description;
        ev.Title = updatedEvent.Title;
        ev.StartAt = updatedEvent.StartAt;
        ev.EndAt = updatedEvent.EndAt;

        return true;
    }

    public bool Delete(Guid id)
    {
        var ev = GetById(id);

        return ev is not null && _events.Remove(ev);
    }
}