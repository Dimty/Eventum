namespace Eventum.Bookings.Domain;

public class Booking
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public Guid UserId { get; private set; }
    public int Seats { get; private set; }
    public BookingStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    private Booking()
    {
    }

    public Booking(Guid eventId, Guid userId, int seats = 1)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException("Event id is required", nameof(eventId));

        if (userId == Guid.Empty)
            throw new ArgumentException("User id is required", nameof(userId));

        if (seats <= 0)
            throw new ArgumentException("Seats must be greater than zero", nameof(seats));

        Id = Guid.NewGuid();
        EventId = eventId;
        UserId = userId;
        Seats = seats;
        Status = BookingStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void Confirm()
    {
        if (Status == BookingStatus.Cancelled)
            return;

        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        if (Status == BookingStatus.Cancelled)
            return;

        Status = BookingStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == BookingStatus.Cancelled)
            throw new BookingAlreadyCancelledException(Id);

        Status = BookingStatus.Cancelled;
        ProcessedAt = DateTime.UtcNow;
    }
}
