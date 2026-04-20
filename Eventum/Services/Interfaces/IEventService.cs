using Eventum.DTO;
using Eventum.Models;

namespace Eventum.Services.Interfaces;

public interface IEventService
{
    PaginatedResult<Event> GetAll(string? title, DateTime? from, DateTime? to, int page = 1, int pageSize = 10);

    Event? GetById(Guid id);

    Event Create(CreateEventDto newEvent);

    bool Update(Guid id, UpdateEventDto updatedEvent);

    bool Delete(Guid id);
}