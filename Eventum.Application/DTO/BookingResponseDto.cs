using Eventum.Domain.Enums;

namespace Eventum.Application.DTO;

public class BookingResponseDto
{
    public Guid Id { get; init; }
    
    public Guid EventId { get; init; }
    
    public BookingStatus Status { get; set; }
    
    public DateTime CreatedAt { get; init; }
    
    public DateTime? ProcessedAt { get; set; }

}