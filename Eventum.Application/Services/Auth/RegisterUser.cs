using Eventum.Application.Common;
using Eventum.Application.DTO;
using Eventum.Application.Exceptions;
using Eventum.Application.Interfaces.Repositories;
using Eventum.Domain.Enums;
using Eventum.Domain.Models;

namespace Eventum.Application.Services.Auth;

public class RegisterUser
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    
    public RegisterUser(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }
    
    public async Task Execute(RegisterRequest request)
    {
        var existingUser = await _userRepository.GetByLoginAsync(request.Login);
        if (existingUser != null)
            throw new UserAlreadyExistsException(request.Login);
        
        var role = Enum.TryParse<UserRole>(request.Role ?? "User", out var parsedRole) 
            ? parsedRole 
            : UserRole.User;
        
        var user = new User(request.Login, _passwordHasher.Hash(request.Password), role);
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();
    }
}