using System.Security.Authentication;
using Eventum.Users.Application.Common;
using Eventum.Users.Application.DTO;
using Eventum.Users.Application.Interfaces;

namespace Eventum.Users.Application.Services;

public class LoginUser(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator)
{
    public async Task<AuthResponse> Execute(LoginRequest request, CancellationToken token = default)
    {
        var user = await userRepository.GetByLoginAsync(request.Login, token);
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new InvalidCredentialException("Invalid credentials");

        return new AuthResponse(tokenGenerator.GenerateToken(user));
    }
}
