namespace Eventum.Users.Application.Exceptions;

public class UserAlreadyExistsException(string login)
    : Exception($"User with login '{login}' already exists");
