namespace Eventum.Application.Exceptions;

public class UnauthorizedAccessException : ApplicationException
{
    public Guid UserId { get; }
    public Guid BookingId { get; }
    
    public UnauthorizedAccessException(Guid userId, Guid bookingId) 
        : base($"User {userId} is not authorized to access booking {bookingId}")
    {
        UserId = userId;
        BookingId = bookingId;
    }
    
    public UnauthorizedAccessException(Guid userId, Guid bookingId, string operation) 
        : base($"User {userId} is not authorized to {operation} booking {bookingId}")
    {
        UserId = userId;
        BookingId = bookingId;
    }
}