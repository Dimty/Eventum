namespace Eventum.Application.Exceptions;

public class BusinessRuleViolationException(string ruleName, string message) : ApplicationException(message)
{
    public string RuleName { get; } = ruleName;
}