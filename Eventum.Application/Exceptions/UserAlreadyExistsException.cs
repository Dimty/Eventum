namespace Eventum.Application.Exceptions;

public class UserAlreadyExistsException(string login)
    : ApplicationException($"User with login '{login}' already exists")
{
    public string Login { get; } = login;
}