namespace Eventum.Domain.Exceptions;

public class BookingAlreadyCancelledException:DomainException
{
    public Guid BookingId { get; }
    public DateTime CancelledAt { get; }
    public string NewCancellationReason { get; }

    public BookingAlreadyCancelledException(
        Guid bookingId, 
        DateTime cancelledAt, 
        string newReason = null!) 
        : base($"Booking {bookingId} was already cancelled at {cancelledAt:yyyy-MM-dd HH:mm:ss}")
    {
        BookingId = bookingId;
        CancelledAt = cancelledAt;
        NewCancellationReason = newReason;
    }

    public override int StatusCode => 400;
}