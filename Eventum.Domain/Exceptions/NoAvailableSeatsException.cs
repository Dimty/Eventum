namespace Eventum.Domain.Exceptions;

public class NoAvailableSeatsException(Guid eventId) : DomainException($"No available seats for event with ID {eventId}")
{
    public Guid EventId { get; } = eventId;
}