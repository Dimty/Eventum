using Eventum.Events.Application.DTO;
using Eventum.Events.Application.Exceptions;
using Eventum.Events.Application.Interfaces;
using Eventum.Events.Domain;

namespace Eventum.Events.Application.Services;

public class EventService(IEventRepository eventRepository) : IEventService
{
    public Task<PaginatedResult<Event>> GetAllAsync(
        string? title,
        DateTime? from,
        DateTime? to,
        int page = 1,
        int pageSize = 10,
        CancellationToken token = default)
    {
        return eventRepository.GetAllAsync(title, from, to, page, pageSize, token);
    }

    public async Task<Event> GetByIdAsync(Guid id, CancellationToken token = default)
    {
        return await eventRepository.GetByIdAsync(id, token)
            ?? throw new ResourceNotFoundException(nameof(Event), id);
    }

    public async Task<Event> CreateAsync(CreateEventDto newEvent, CancellationToken token = default)
    {
        var ev = Event.Create(
            newEvent.Title,
            newEvent.Description,
            newEvent.StartAt,
            newEvent.EndAt,
            newEvent.TotalSeats);

        await eventRepository.AddAsync(ev, token);
        await eventRepository.SaveChangesAsync(token);

        return ev;
    }

    public async Task UpdateAsync(Guid id, UpdateEventDto updatedEvent, CancellationToken token = default)
    {
        var ev = await GetByIdAsync(id, token);
        ev.Update(updatedEvent.Title, updatedEvent.Description, updatedEvent.StartAt, updatedEvent.EndAt);
        await eventRepository.SaveChangesAsync(token);
    }

    public async Task DeleteAsync(Guid id, CancellationToken token = default)
    {
        var ev = await GetByIdAsync(id, token);
        eventRepository.Delete(ev);
        await eventRepository.SaveChangesAsync(token);
    }

    public async Task<bool> ApplyBookingConfirmedAsync(Guid eventId, int seats, CancellationToken token = default)
    {
        var ev = await eventRepository.GetByIdAsync(eventId, token);
        if (ev is null)
            return false;

        var applied = ev.TryDecreaseAvailableSeats(seats);
        if (!applied)
            return false;

        await eventRepository.SaveChangesAsync(token);
        return true;
    }
}
