using Eventum.Bookings.Domain;

namespace Eventum.Bookings.Application.DTO;

public class BookingResponseDto
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public int Seats { get; init; }
    public BookingStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
}
