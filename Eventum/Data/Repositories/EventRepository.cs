using Eventum.Data.Interfaces;
using Eventum.DataAccess.Contexts;
using Eventum.DTO;
using Eventum.Models;
using Microsoft.EntityFrameworkCore;

namespace Eventum.Data.Repositories;

public class EventRepository(AppDbContext context) : IEventRepository
{
    public async Task<PaginatedResult<Event>> GetAllAsync(string? title = null, DateTime? from = null,
        DateTime? to = null,
        int page = 1, int pageSize = 10,
        CancellationToken token = default)
    {
        IQueryable<Event> query = context.Events;

        if (!string.IsNullOrWhiteSpace(title))
            query = query.Where(ev => EF.Functions.Like(ev.Title, $"%{title}%"));

        if (from.HasValue)
            query = query.Where(ev => ev.StartAt >= from.Value);

        if (to.HasValue)
            query = query.Where(ev => ev.EndAt <= to.Value);

        var total = await query.CountAsync(token);

        var items = await query
            .OrderBy(ev => ev.StartAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken: token);

        return new PaginatedResult<Event>
        {
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            Count = items.Count,
            Items = items
        };
    }

    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken token = default) =>
        await context.Events.FirstOrDefaultAsync(ev => ev.Id == id, token);

    public async Task AddAsync(Event ev, CancellationToken token = default) =>
        await context.Events.AddAsync(ev, token);


    public Task DeleteAsync(Event ev, CancellationToken token = default)
    {
        context.Events.Remove(ev);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken token = default) =>
        await context.SaveChangesAsync(token);
}