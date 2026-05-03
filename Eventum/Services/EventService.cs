using Eventum.DataAccess.Contexts;
using Eventum.DTO;
using Eventum.Exceptions;
using Eventum.Models;
using Eventum.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Eventum.Services;

public class EventService(AppDbContext context) : IEventService
{
    private readonly List<Event> _events = new();

    public async Task<PaginatedResult<Event>> GetAllAsync(string? title, DateTime? from, DateTime? to, int page = 1,
        int pageSize = 10)
    {
        IQueryable<Event> query = context.Events;

        if (!string.IsNullOrWhiteSpace(title))
            query = query.Where(ev => EF.Functions.Like(ev.Title, $"%{title}%"));
        

        if (from.HasValue)
            query = query.Where(ev => ev.StartAt >= from.Value);

        if (to.HasValue)
            query = query.Where(ev => ev.EndAt <= to.Value);

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<Event>
        {
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            Count = items.Count,
            Items = items
        };
    }

    public async Task<Event?> GetByIdAsync(Guid id)
    {
        var ev = await context.Events.FirstOrDefaultAsync(e => e.Id == id);

        if (ev == null)
            throw new NotFoundException($"Event with id {id} not found");

        return ev;
    }

    public async Task<Event> CreateAsync(CreateEventDto newEvent)
    {
        var ev = Event.Create(
            newEvent.Title,
            newEvent.Description,
            newEvent.StartAt,
            newEvent.EndAt,
            newEvent.TotalSeats!.Value);

        await context.Events.AddAsync(ev);
        await context.SaveChangesAsync();
        
        return ev;
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateEventDto updatedEvent)
    {
        var ev = (await GetByIdAsync(id))!;

        ev.Update(
            updatedEvent.Title, 
            updatedEvent.Description, 
            updatedEvent.StartAt, 
            updatedEvent.EndAt);
        
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var ev = await context.Events.FirstOrDefaultAsync(e => e.Id == id);

        if (ev == null)
            throw new NotFoundException($"Event with id {id} not found");

        context.Events.Remove(ev);
        await context.SaveChangesAsync();

        return true;
    }
}