using Eventum.Events.Application.DTO;
using Eventum.Events.Application.Interfaces;
using Eventum.Events.Domain;
using Microsoft.EntityFrameworkCore;

namespace Eventum.Events.Infrastructure.Data;

public class EventRepository(EventsDbContext context) : IEventRepository
{
    public async Task<PaginatedResult<Event>> GetAllAsync(
        string? title = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken token = default)
    {
        IQueryable<Event> query = context.Events;

        if (!string.IsNullOrWhiteSpace(title))
            query = query.Where(ev => EF.Functions.ILike(ev.Title, $"%{title}%"));

        if (from.HasValue)
            query = query.Where(ev => ev.StartAt >= from.Value);

        if (to.HasValue)
            query = query.Where(ev => ev.EndAt <= to.Value);

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var total = await query.CountAsync(token);
        var items = await query
            .OrderBy(ev => ev.StartAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(token);

        return new PaginatedResult<Event>
        {
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            Count = items.Count,
            Items = items
        };
    }

    public Task<Event?> GetByIdAsync(Guid id, CancellationToken token = default) =>
        context.Events.FirstOrDefaultAsync(ev => ev.Id == id, token);

    public async Task AddAsync(Event ev, CancellationToken token = default) =>
        await context.Events.AddAsync(ev, token);

    public void Delete(Event ev) => context.Events.Remove(ev);

    public Task SaveChangesAsync(CancellationToken token = default) =>
        context.SaveChangesAsync(token);
}
