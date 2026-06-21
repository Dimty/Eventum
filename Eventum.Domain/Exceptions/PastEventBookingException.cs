namespace Eventum.Domain.Exceptions;

public class PastEventBookingException: DomainException
{
    public override int StatusCode => 400;
    
    public PastEventBookingException(string eventId, DateTime eventDate) 
        : base($"Cannot book event with ID {eventId} because it already started on {eventDate:yyyy-MM-dd HH:mm}")
    {
    }
    
    public PastEventBookingException(string message) : base(message)
    {
    }

}