namespace Eventum.Application.Exceptions;

public class ResourceNotFoundException(string resourceName, object resourceId)
    : ApplicationException($"{resourceName} with id {resourceId} not found")
{
    public string ResourceName { get; } = resourceName;
    public object ResourceId { get; } = resourceId;
}