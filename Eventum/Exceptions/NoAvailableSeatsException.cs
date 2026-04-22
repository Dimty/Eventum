namespace Eventum.Exceptions;

public class NoAvailableSeatsException() : 
    AppException("No available seats for this event", StatusCodes.Status409Conflict)
{
    
}