using System.Text.Json;
using Eventum.Events.Application.Caching;
using Eventum.Events.Application.DTO;
using Eventum.Events.Application.Exceptions;
using Eventum.Events.Application.Interfaces;
using Eventum.Events.Application.Options;
using Eventum.Events.Domain;

namespace Eventum.Events.Application.Services;

public class EventService(
    IEventRepository eventRepository,
    ICacheService? cache = null,
    EventCacheSettings? cacheSettings = null) : IEventService
{
    private static readonly JsonSerializerOptions CacheJsonOptions = new(JsonSerializerDefaults.Web);
    private readonly EventCacheSettings cacheSettings = cacheSettings ?? new EventCacheSettings();

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
        var cacheKey = EventCacheKeys.Event(id);
        var cached = cache is null ? null : await cache.GetStringAsync(cacheKey, token);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            try
            {
                var cacheEntry = JsonSerializer.Deserialize<EventCacheEntry>(cached, CacheJsonOptions);
                if (cacheEntry is not null)
                    return cacheEntry.ToEvent();
            }
            catch (JsonException)
            {
                await RemoveEventCacheAsync(id, token);
            }
        }

        var ev = await eventRepository.GetByIdAsync(id, token)
            ?? throw new ResourceNotFoundException(nameof(Event), id);

        await SetEventCacheAsync(ev, token);
        return ev;
    }

    public async Task<IReadOnlyList<Event>> GetTopAsync(CancellationToken token = default)
    {
        var cached = cache is null ? null : await cache.GetStringAsync(EventCacheKeys.Top10, token);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            try
            {
                var cacheEntries = JsonSerializer.Deserialize<List<EventCacheEntry>>(cached, CacheJsonOptions);
                if (cacheEntries is not null)
                    return cacheEntries.Select(entry => entry.ToEvent()).ToList();
            }
            catch (JsonException)
            {
                await RemoveTopEventsCacheAsync(token);
            }
        }

        var events = await eventRepository.GetTopBySoldSeatsPercentageAsync(10, token);
        await SetTopEventsCacheAsync(events, token);
        return events;
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
        await SetEventCacheAsync(ev, token);

        return ev;
    }

    public async Task UpdateAsync(Guid id, UpdateEventDto updatedEvent, CancellationToken token = default)
    {
        var ev = await eventRepository.GetByIdAsync(id, token)
            ?? throw new ResourceNotFoundException(nameof(Event), id);

        ev.Update(updatedEvent.Title, updatedEvent.Description, updatedEvent.StartAt, updatedEvent.EndAt);
        await eventRepository.SaveChangesAsync(token);
        await RemoveEventCacheAsync(id, token);
    }

    public async Task DeleteAsync(Guid id, CancellationToken token = default)
    {
        var ev = await eventRepository.GetByIdAsync(id, token)
            ?? throw new ResourceNotFoundException(nameof(Event), id);

        eventRepository.Delete(ev);
        await eventRepository.SaveChangesAsync(token);
        await RemoveEventCacheAsync(id, token);
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
        await RemoveEventCacheAsync(eventId, token);
        return true;
    }

    private Task SetEventCacheAsync(Event ev, CancellationToken token)
    {
        if (cache is null)
            return Task.CompletedTask;

        var cacheEntry = EventCacheEntry.FromEvent(ev);
        var value = JsonSerializer.Serialize(cacheEntry, CacheJsonOptions);
        var ttl = TimeSpan.FromSeconds(cacheSettings.EventTtlSeconds);

        return cache.SetStringAsync(EventCacheKeys.Event(ev.Id), value, ttl, token);
    }

    private Task SetTopEventsCacheAsync(IReadOnlyList<Event> events, CancellationToken token)
    {
        if (cache is null)
            return Task.CompletedTask;

        var cacheEntries = events.Select(EventCacheEntry.FromEvent).ToList();
        var value = JsonSerializer.Serialize(cacheEntries, CacheJsonOptions);
        var ttl = TimeSpan.FromSeconds(cacheSettings.TopEventsTtlSeconds);

        return cache.SetStringAsync(EventCacheKeys.Top10, value, ttl, token);
    }

    private Task RemoveEventCacheAsync(Guid id, CancellationToken token) =>
        cache?.RemoveAsync(EventCacheKeys.Event(id), token) ?? Task.CompletedTask;

    private Task RemoveTopEventsCacheAsync(CancellationToken token) =>
        cache?.RemoveAsync(EventCacheKeys.Top10, token) ?? Task.CompletedTask;
}
