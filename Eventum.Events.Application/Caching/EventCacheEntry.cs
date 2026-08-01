using Eventum.Events.Domain;

namespace Eventum.Events.Application.Caching;

public sealed record EventCacheEntry(
    Guid Id,
    string Title,
    string? Description,
    DateTime StartAt,
    DateTime EndAt,
    int TotalSeats,
    int AvailableSeats)
{
    public static EventCacheEntry FromEvent(Event ev) => new(
        ev.Id,
        ev.Title,
        ev.Description,
        ev.StartAt,
        ev.EndAt,
        ev.TotalSeats,
        ev.AvailableSeats);

    public Event ToEvent() => Event.Restore(
        Id,
        Title,
        Description,
        StartAt,
        EndAt,
        TotalSeats,
        AvailableSeats);
}
