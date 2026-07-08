using Eventum.Users.Application.Common;
using Eventum.Users.Application.DTO;
using Eventum.Users.Application.Exceptions;
using Eventum.Users.Application.Interfaces;
using Eventum.Users.Domain;

namespace Eventum.Users.Application.Services;

public class RegisterUser(IUserRepository userRepository, IPasswordHasher passwordHasher)
{
    public async Task Execute(RegisterRequest request, CancellationToken token = default)
    {
        var existingUser = await userRepository.GetByLoginAsync(request.Login, token);
        if (existingUser is not null)
            throw new UserAlreadyExistsException(request.Login);

        var role = Enum.TryParse<UserRole>(request.Role ?? nameof(UserRole.User), ignoreCase: true, out var parsedRole)
            ? parsedRole
            : UserRole.User;

        var user = new User(request.Login, passwordHasher.Hash(request.Password), role);
        await userRepository.AddAsync(user, token);
        await userRepository.SaveChangesAsync(token);
    }
}
