using Eventum.Events.Application.DTO;
using Eventum.Events.Domain;

namespace Eventum.Events.Application.Interfaces;

public interface IEventRepository
{
    Task<PaginatedResult<Event>> GetAllAsync(
        string? title = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken token = default);

    Task<Event?> GetByIdAsync(Guid id, CancellationToken token = default);

    Task<IReadOnlyList<Event>> GetTopBySoldSeatsPercentageAsync(int count = 10, CancellationToken token = default);

    Task AddAsync(Event ev, CancellationToken token = default);

    void Delete(Event ev);

    Task SaveChangesAsync(CancellationToken token = default);
}
