using Eventum.Models;

namespace Eventum.Services.Interfaces;

public interface IEventService
{
    IEnumerable<Event> GetAll();

    Event? GetById(Guid id);

    Event Create(Event newEvent);

    bool Update(Guid id, Event updatedEvent);

    bool Delete(Guid id);
}