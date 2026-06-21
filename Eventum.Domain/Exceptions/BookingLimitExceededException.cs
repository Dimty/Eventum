namespace Eventum.Domain.Exceptions;

public class BookingLimitExceededException : DomainException
{
    public override int StatusCode => 409;
    
    public string UserId { get; }
    public int CurrentBookingCount { get; }
    public int MaxAllowedBookings { get; }
    
    public BookingLimitExceededException(string userId, int currentCount, int maxAllowed) 
        : base($"User {userId} has {currentCount} active bookings, which exceeds the limit of {maxAllowed}")
    {
        UserId = userId;
        CurrentBookingCount = currentCount;
        MaxAllowedBookings = maxAllowed;
    }
    
    public BookingLimitExceededException(string message) : base(message)
    {
    }
}