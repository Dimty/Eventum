using Eventum.Domain.Enums;
using Eventum.Domain.Exceptions;

namespace Eventum.Domain.Models;

public class Booking
{
    public Guid Id { get; private set; }
    
    public Guid EventId { get; private set; }
    
    public BookingStatus Status { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    
    public DateTime? ProcessedAt { get; private set; }

    public Event Event { get; private set; } = null!;
    
    public Guid UserId { get; private set; }
    
    public User? User { get; private set; }

    private Booking() { }

    public Booking(Guid eventId, Guid userId)
    {
        Id = Guid.NewGuid();
        EventId = eventId;
        CreatedAt = DateTime.UtcNow;
        Status = BookingStatus.Pending;
        UserId = userId;
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
    
    public void Cancel()
    {
        if(Status == BookingStatus.Cancelled) throw new BookingAlreadyCancelledException(Id, ProcessedAt ?? DateTime.UtcNow);
        
        if (Event == null)
            throw new InvalidOperationException("Event must be loaded before cancelling booking");
        
        Event.ReleaseSeats();
        
        Status = BookingStatus.Cancelled;
        ProcessedAt = DateTime.UtcNow;
    }

}