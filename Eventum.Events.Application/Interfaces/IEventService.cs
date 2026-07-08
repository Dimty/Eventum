using Eventum.Events.Application.DTO;
using Eventum.Events.Domain;

namespace Eventum.Events.Application.Interfaces;

public interface IEventService
{
    Task<PaginatedResult<Event>> GetAllAsync(string? title, DateTime? from, DateTime? to, int page = 1, int pageSize = 10, CancellationToken token = default);

    Task<Event> GetByIdAsync(Guid id, CancellationToken token = default);

    Task<Event> CreateAsync(CreateEventDto newEvent, CancellationToken token = default);

    Task UpdateAsync(Guid id, UpdateEventDto updatedEvent, CancellationToken token = default);

    Task DeleteAsync(Guid id, CancellationToken token = default);

    Task<bool> ApplyBookingConfirmedAsync(Guid eventId, int seats, CancellationToken token = default);
}
