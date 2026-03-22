using Eventum.DTO;
using Eventum.Models;
using Eventum.Services.Interfaces;

namespace Eventum.Services;

public class EventService : IEventService
{
    private readonly List<Event> _events = new();

    public PaginatedResult<Event> GetAll(string? title, DateTime? from, DateTime? to, int page = 1, int pageSize = 10)
    {
        var query = _events.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(title))
            query = query.Where(ev => ev.Title.Contains(title, StringComparison.OrdinalIgnoreCase));

        if (from.HasValue)
            query = query.Where(ev => ev.StartAt >= from.Value);

        if (to.HasValue)
            query = query.Where(ev => ev.EndAt <= to.Value);

        var total = query.Count();

        var items = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        
        return new PaginatedResult<Event>
        {
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            Count = items.Count,
            Items = items
        };
    }

    public Event? GetById(Guid id)
    {
        var ev = _events.FirstOrDefault(e => e.Id == id);

        if (ev == null)
            throw new KeyNotFoundException($"Event with id {id} not found");

        return ev;
    }

    public Event Create(Event newEvent)
    {
        newEvent.Id = Guid.NewGuid();
        _events.Add(newEvent);
        return newEvent;
    }

    public bool Update(Guid id, Event updatedEvent)
    {
        var ev = GetById(id)!;

        ev.Description = updatedEvent.Description;
        ev.Title = updatedEvent.Title;
        ev.StartAt = updatedEvent.StartAt;
        ev.EndAt = updatedEvent.EndAt;

        return true;
    }

    public bool Delete(Guid id)
    {
        var ev = GetById(id);

        return ev is not null && _events.Remove(ev);
    }
}