using Eventum.Users.Domain;
using Eventum.Users.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Eventum.Users.IntegrationTests;

public class UserRepositoryIntegrationTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task AddAsync_ShouldPersistUser()
    {
        var token = TestContext.Current.CancellationToken;
        await using var context = await CreateContextAsync(token);
        var repository = new UserRepository(context);
        var user = new User("user", "hash", UserRole.User);

        await repository.AddAsync(user, token);
        await repository.SaveChangesAsync(token);

        context.ChangeTracker.Clear();
        var saved = await repository.GetByLoginAsync("user", token);

        Assert.NotNull(saved);
        Assert.Equal(user.Id, saved.Id);
        Assert.Equal(UserRole.User, saved.Role);
    }

    [Fact]
    public async Task GetByLoginAsync_ShouldReturnExactUser_WhenSeveralUsersExist()
    {
        var token = TestContext.Current.CancellationToken;
        await using var context = await CreateContextAsync(token);
        var repository = new UserRepository(context);
        var user = new User("target", "hash", UserRole.Admin);

        await repository.AddAsync(new User("other", "hash", UserRole.User), token);
        await repository.AddAsync(user, token);
        await repository.SaveChangesAsync(token);

        context.ChangeTracker.Clear();
        var saved = await repository.GetByLoginAsync("target", token);

        Assert.NotNull(saved);
        Assert.Equal(user.Id, saved.Id);
        Assert.Equal(UserRole.Admin, saved.Role);
    }

    [Fact]
    public async Task GetByLoginAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        var token = TestContext.Current.CancellationToken;
        await using var context = await CreateContextAsync(token);
        var repository = new UserRepository(context);

        var result = await repository.GetByLoginAsync("missing", token);

        Assert.Null(result);
    }

    private async Task<UsersDbContext> CreateContextAsync(CancellationToken token)
    {
        var connectionString = new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
        {
            Database = $"eventum_users_{Guid.NewGuid():N}"
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        var context = new UsersDbContext(options);
        await context.Database.EnsureCreatedAsync(token);
        return context;
    }
}
