using Eventum.Application.DTO;
using Eventum.Domain.Models;

namespace Eventum.Application.Interfaces.Repositories;

public interface IEventRepository
{
    Task<PaginatedResult<Event>> GetAllAsync(string? title = null, DateTime? from = null, DateTime? to = null, int page = 1,
        int pageSize = 10, CancellationToken token = default);

    Task<Event?> GetByIdAsync(Guid id, CancellationToken token = default);
    Task AddAsync(Event ev, CancellationToken token = default);
    Task DeleteAsync(Event ev, CancellationToken token = default);
    Task SaveChangesAsync(CancellationToken token = default);
}