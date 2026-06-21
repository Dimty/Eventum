using Eventum.Domain.Exceptions;

namespace Eventum.Application.Exceptions;

public static class ExceptionMapper
{
    public static ApplicationException Map(DomainException domainException) 
        => domainException switch
        {
            NoAvailableSeatsException ex => 
                new BusinessRuleViolationException(
                    "SeatReservation", 
                    $"No available seats for event {ex.EventId}"),
            
            EntityNotFoundException ex => 
                new ResourceNotFoundException(ex.EntityName, ex.EntityId),
            
            _ => new UnknownApplicationException(domainException.Message)
        };
}