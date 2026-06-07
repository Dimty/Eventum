namespace Eventum.Domain.Exceptions;

public class EntityNotFoundException(string entityName, object entityId)
    : DomainException($"{entityName} with ID {entityId} not found")
{
    public string EntityName { get; } = entityName;
    public object EntityId { get; } = entityId;
}