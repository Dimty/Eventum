using Eventum.Data.Interfaces;
using Eventum.DataAccess.Contexts;
using Eventum.DTO;
using Eventum.Exceptions;
using Eventum.Models;
using Eventum.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Eventum.Services;

public class EventService(IEventRepository eventRepository) : IEventService
{
    public async Task<PaginatedResult<Event>> GetAllAsync(string? title, DateTime? from, DateTime? to, int page = 1,
        int pageSize = 10)
    {
        return await eventRepository.GetAllAsync(title, from, to, page, pageSize);
    }

    public async Task<Event?> GetByIdAsync(Guid id)
    {
        var ev = await eventRepository.GetByIdAsync(id);
        
        return ev ?? throw new NotFoundException($"Event with id {id} not found");
    }

    public async Task<Event> CreateAsync(CreateEventDto newEvent)
    {
        var ev = Event.Create(
            newEvent.Title,
            newEvent.Description,
            newEvent.StartAt,
            newEvent.EndAt,
            newEvent.TotalSeats!.Value);

        await eventRepository.AddAsync(ev);
        await eventRepository.SaveChangesAsync();
        
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
        
        await eventRepository.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var ev = await eventRepository.GetByIdAsync(id);

        if (ev == null)
            throw new NotFoundException($"Event with id {id} not found");

        await eventRepository.DeleteAsync(ev);
        await eventRepository.SaveChangesAsync();

        return true;
    }
}