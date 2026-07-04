using System.Security.Authentication;
using Eventum.Application.Common;
using Eventum.Application.DTO;
using Eventum.Application.Interfaces.Repositories;

namespace Eventum.Application.Services.Auth;

public class LoginUser
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;
    
    public LoginUser(
        IUserRepository userRepository, 
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }
    
    public async Task<AuthResponse> Execute(LoginRequest request)
    {
        var user = await _userRepository.GetByLoginAsync(request.Login);
        if (user == null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new InvalidCredentialException("Invalid credentials");
        
        var token = _tokenGenerator.GenerateToken(user);
        return new AuthResponse(token);
    }
}