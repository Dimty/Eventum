using System.Security.Authentication;
using System.Security.Claims;
using Eventum.Users.Application.Common;
using Eventum.Users.Application.DTO;
using Eventum.Users.Application.Exceptions;
using Eventum.Users.Application.Interfaces;
using Eventum.Users.Application.Services;
using Eventum.Users.Domain;
using Eventum.Users.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Eventum.Users.Infrastructure.Options;
using System.IdentityModel.Tokens.Jwt;

namespace Eventum.Users.Tests;

public class UserAuthServiceTests
{
    [Fact]
    public async Task Register_ShouldCreateUser_WithDefaultRole()
    {
        var token = TestContext.Current.CancellationToken;
        var repository = new InMemoryUserRepository();
        var service = new RegisterUser(repository, new PasswordHasher());

        await service.Execute(new RegisterRequest("user", "password", null), token);

        var user = await repository.GetByLoginAsync("user", token);
        Assert.NotNull(user);
        Assert.Equal(UserRole.User, user.Role);
        Assert.NotEqual("password", user.PasswordHash);
    }

    [Fact]
    public async Task Register_ShouldCreateAdmin_WhenRoleIsAdmin()
    {
        var token = TestContext.Current.CancellationToken;
        var repository = new InMemoryUserRepository();
        var service = new RegisterUser(repository, new PasswordHasher());

        await service.Execute(new RegisterRequest("admin", "password", "Admin"), token);

        var user = await repository.GetByLoginAsync("admin", token);
        Assert.NotNull(user);
        Assert.Equal(UserRole.Admin, user.Role);
    }

    [Fact]
    public async Task Register_ShouldThrow_WhenLoginAlreadyExists()
    {
        var token = TestContext.Current.CancellationToken;
        var repository = new InMemoryUserRepository();
        var service = new RegisterUser(repository, new PasswordHasher());
        await service.Execute(new RegisterRequest("user", "password", null), token);

        await Assert.ThrowsAsync<UserAlreadyExistsException>(() =>
            service.Execute(new RegisterRequest("user", "password", null), token));
    }

    [Fact]
    public async Task Register_ShouldFallbackToUser_WhenRoleIsUnknown()
    {
        var token = TestContext.Current.CancellationToken;
        var repository = new InMemoryUserRepository();
        var service = new RegisterUser(repository, new PasswordHasher());

        await service.Execute(new RegisterRequest("user", "password", "SuperAdmin"), token);

        var user = await repository.GetByLoginAsync("user", token);
        Assert.NotNull(user);
        Assert.Equal(UserRole.User, user.Role);
    }

    [Fact]
    public async Task Login_ShouldReturnJwt_WhenCredentialsAreValid()
    {
        var token = TestContext.Current.CancellationToken;
        var repository = new InMemoryUserRepository();
        var hasher = new PasswordHasher();
        var register = new RegisterUser(repository, hasher);
        var login = new LoginUser(repository, hasher, CreateTokenGenerator());

        await register.Execute(new RegisterRequest("user", "password", null), token);

        var result = await login.Execute(new LoginRequest("user", "password"), token);

        Assert.False(string.IsNullOrWhiteSpace(result.Token));
    }

    [Fact]
    public async Task Login_ShouldIncludeIdentityAndRoleClaims()
    {
        var token = TestContext.Current.CancellationToken;
        var repository = new InMemoryUserRepository();
        var hasher = new PasswordHasher();
        var register = new RegisterUser(repository, hasher);
        var login = new LoginUser(repository, hasher, CreateTokenGenerator());

        await register.Execute(new RegisterRequest("admin", "password", "Admin"), token);

        var result = await login.Execute(new LoginRequest("admin", "password"), token);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        Assert.Equal("admin", jwt.Claims.First(claim => claim.Type == ClaimTypes.Name).Value);
        Assert.Equal(UserRole.Admin.ToString(), jwt.Claims.First(claim => claim.Type == ClaimTypes.Role).Value);
        Assert.False(string.IsNullOrWhiteSpace(jwt.Claims.First(claim => claim.Type == ClaimTypes.NameIdentifier).Value));
    }

    [Fact]
    public async Task Login_ShouldThrow_WhenPasswordIsInvalid()
    {
        var token = TestContext.Current.CancellationToken;
        var repository = new InMemoryUserRepository();
        var hasher = new PasswordHasher();
        var register = new RegisterUser(repository, hasher);
        var login = new LoginUser(repository, hasher, CreateTokenGenerator());
        await register.Execute(new RegisterRequest("user", "password", null), token);

        await Assert.ThrowsAsync<InvalidCredentialException>(() =>
            login.Execute(new LoginRequest("user", "wrong"), token));
    }

    [Fact]
    public async Task Login_ShouldThrow_WhenUserDoesNotExist()
    {
        var token = TestContext.Current.CancellationToken;
        var repository = new InMemoryUserRepository();
        var service = new LoginUser(repository, new PasswordHasher(), CreateTokenGenerator());

        await Assert.ThrowsAsync<InvalidCredentialException>(() =>
            service.Execute(new LoginRequest("missing", "password"), token));
    }

    private static ITokenGenerator CreateTokenGenerator()
    {
        var settings = Options.Create(new JwtSettings
        {
            Secret = "your-super-secret-key-here-and-it-should-be-more-than-256-bit",
            Issuer = "Eventum",
            Audience = "Eventum",
            ExpirationMinutes = 15
        });

        return new JwtTokenGenerator(settings);
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        private readonly List<User> _users = [];

        public Task<User?> GetByLoginAsync(string login, CancellationToken token = default) =>
            Task.FromResult(_users.FirstOrDefault(user => user.Login == login));

        public Task AddAsync(User user, CancellationToken token = default)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken token = default) => Task.CompletedTask;
    }
}
