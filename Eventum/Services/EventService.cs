using Eventum.Models;
using Eventum.Services.Interfaces;

namespace Eventum.Services;

public class EventService: IEventService
{
    public IEnumerable<Event> GetAll()
    {
        throw new NotImplementedException();
    }

    public Event? GetById(Guid id)
    {
        throw new NotImplementedException();
    }

    public Event Create(Event newEvent)
    {
        throw new NotImplementedException();
    }

    public bool Update(Guid id, Event updatedEvent)
    {
        throw new NotImplementedException();
    }

    public bool Delete(Guid id)
    {
        throw new NotImplementedException();
    }
}