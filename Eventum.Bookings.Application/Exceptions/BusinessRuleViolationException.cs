namespace Eventum.Bookings.Application.Exceptions;

public class BusinessRuleViolationException(string ruleName, string message) : Exception(message)
{
    public string RuleName { get; } = ruleName;
}
