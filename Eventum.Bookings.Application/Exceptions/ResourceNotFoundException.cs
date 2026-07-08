namespace Eventum.Bookings.Application.Exceptions;

public class ResourceNotFoundException(string resource, Guid id)
    : Exception($"{resource} with id '{id}' was not found");
