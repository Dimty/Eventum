using Eventum.Domain.Enums;

namespace Eventum.Domain.Models;

public class User
{
    public Guid Id { get; private set; }
    public string Login { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }

    private User() { }

    public User(string login, string passwordHash, UserRole role = UserRole.User)
    {
        Id = Guid.NewGuid();
        Login = login;
        PasswordHash = passwordHash;
        Role = role;
    }
    
    public void ChangeRole(UserRole newRole)
    {
        Role = newRole;
    }
    
}