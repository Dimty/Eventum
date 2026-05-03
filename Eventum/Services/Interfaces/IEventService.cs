using Eventum.DTO;
using Eventum.Models;

namespace Eventum.Services.Interfaces;

public interface IEventService
{
    Task<PaginatedResult<Event>> GetAllAsync(string? title, DateTime? from, DateTime? to, int page = 1, int pageSize = 10);

    Task<Event?> GetByIdAsync(Guid id);

    Task<Event> CreateAsync(CreateEventDto newEvent);

    Task<bool> UpdateAsync(Guid id, UpdateEventDto updatedEvent);

    Task<bool> DeleteAsync(Guid id);
}