using Eventum.Domain.Enums;
using Eventum.Domain.Models;
using Eventum.Infrastructure.Data.Repositories;
using Eventum.IntegrationTests.Base;
using Eventum.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Eventum.IntegrationTests;

[Collection("Database collection")]
public class UserRepositoryIntegrationTests(DatabaseCollectionFixture fixture) : DatabaseTestBase(fixture)
{
    private async Task<User> CreateUserAsync(Guid id, string login = "login", string password = "password",
        UserRole role = UserRole.User)
    {
        var context = CreateContext();

        var user = new User(login, password, role);

        typeof(User).GetProperty("Id")!.SetValue(user, id);

        context.Users.Add(user);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return user;
    }

    [Fact]
    public async Task GetByLoginAsync_ExistingUser_ShouldReturnUser()
    {
        // Arrange
        await ResetDatabaseAsync();

        var context = CreateContext();
        var user = await CreateUserAsync(Guid.NewGuid(), login: "testuser");
        var repo = new UserRepository(context);

        // Act
        var result = await repo.GetByLoginAsync("testuser", TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.Login, result.Login);
        Assert.Equal(user.PasswordHash, result.PasswordHash);
    }

    [Fact]
    public async Task GetByLoginAsync_NonExistingUser_ShouldReturnNull()
    {
        // Arrange
        await ResetDatabaseAsync();

        var context = CreateContext();
        await CreateUserAsync(Guid.NewGuid(), login: "testuser");
        var repo = new UserRepository(context);

        // Act
        var result = await repo.GetByLoginAsync("anotheruser", TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByLoginAsync_WithMultipleUsers_ShouldReturnCorrectUser()
    {
        // Arrange
        await ResetDatabaseAsync();

        var context = CreateContext();
        var user1 = await CreateUserAsync(Guid.NewGuid(), login: "user1");
        var user2 = await CreateUserAsync(Guid.NewGuid(), login: "user2");
        var user3 = await CreateUserAsync(Guid.NewGuid(), login: "user3");

        var repo = new UserRepository(context);

        // Act
        var result = await repo.GetByLoginAsync("user2", TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user2.Id, result.Id);
        Assert.Equal("user2", result.Login);
    }
}