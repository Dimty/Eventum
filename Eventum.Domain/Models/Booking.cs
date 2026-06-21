namespace Eventum.Domain.Models;

public class Booking
{
    public Guid Id { get; init; }
    
    public Guid EventId { get; set; }
    
    public BookingStatus Status { get; set; }
    
    public DateTime CreatedAt { get; init; }
    
    public DateTime? ProcessedAt { get; set; }

    public Event Event { get; set; } = null!;

    private Booking() { }

    public Booking(Guid eventId)
    {
        Id = Guid.NewGuid();
        EventId = eventId;
        CreatedAt = DateTime.UtcNow;
        Status = BookingStatus.Pending;
    }

    public void Confirm()
    {
        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;
    }
    
    public void Reject()
    {
        Status = BookingStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
    }

}