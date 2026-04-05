namespace Eventum.Exceptions;

public class NotFoundException(string message) : AppException(message, StatusCodes.Status404NotFound)
{ }