using Eventum.Users.Domain;
using Eventum.Users.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Eventum.Users.IntegrationTests;

public class DatabaseSchemaIntegrationTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task Database_ShouldHaveUsersTable_WithExpectedColumns()
    {
        var token = TestContext.Current.CancellationToken;
        await using var context = await CreateContextAsync(token);

        var columns = await context.Database
            .SqlQuery<ColumnInfo>($"""
                SELECT
                    column_name AS "ColumnName",
                    data_type AS "DataType",
                    is_nullable AS "IsNullable",
                    character_maximum_length AS "MaxLength"
                FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = 'users'
                ORDER BY ordinal_position
                """)
            .ToListAsync(token);

        Assert.NotEmpty(columns);
        Assert.Contains(columns, column => column.ColumnName == "id" && column.DataType == "uuid" && column.IsNullable == "NO");
        Assert.Contains(columns, column => column.ColumnName == "login" && column.DataType == "character varying" && column.MaxLength == 200);
        Assert.Contains(columns, column => column.ColumnName == "password_hash" && column.DataType == "text" && column.IsNullable == "NO");
        Assert.Contains(columns, column => column.ColumnName == "role" && column.DataType == "integer" && column.IsNullable == "NO");
    }

    [Fact]
    public async Task Database_ShouldHaveUniqueIndex_ForUserLogin()
    {
        var token = TestContext.Current.CancellationToken;
        await using var context = await CreateContextAsync(token);

        var indexes = await context.Database
            .SqlQuery<string>($"""
                SELECT indexdef
                FROM pg_indexes
                WHERE schemaname = 'public' AND tablename = 'users'
                """)
            .ToListAsync(token);

        Assert.Contains(indexes, index =>
            index.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) &&
            index.Contains("(login)", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Database_ShouldEnforcePrimaryKeyUniqueness_ForUsers()
    {
        var token = TestContext.Current.CancellationToken;
        var databaseName = $"eventum_users_{Guid.NewGuid():N}";
        await using var context = await CreateContextAsync(token, databaseName);
        var existing = new User("user-1", "hash", UserRole.User);
        var duplicate = new User("user-2", "hash", UserRole.Admin);
        var id = Guid.NewGuid();
        SetId(existing, id);
        SetId(duplicate, id);

        context.Users.Add(existing);
        await context.SaveChangesAsync(token);

        await using var newContext = await CreateContextAsync(token, databaseName);
        newContext.Users.Add(duplicate);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
            newContext.SaveChangesAsync(token));

        Assert.Equal("23505", Assert.IsType<PostgresException>(exception.InnerException).SqlState);
    }

    [Fact]
    public async Task Database_ShouldEnforceUniqueLogin_ForUsers()
    {
        var token = TestContext.Current.CancellationToken;
        var databaseName = $"eventum_users_{Guid.NewGuid():N}";
        await using var context = await CreateContextAsync(token, databaseName);

        context.Users.Add(new User("duplicate-login", "hash", UserRole.User));
        await context.SaveChangesAsync(token);

        await using var newContext = await CreateContextAsync(token, databaseName);
        newContext.Users.Add(new User("duplicate-login", "hash", UserRole.Admin));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
            newContext.SaveChangesAsync(token));

        Assert.Equal("23505", Assert.IsType<PostgresException>(exception.InnerException).SqlState);
    }

    private async Task<UsersDbContext> CreateContextAsync(CancellationToken token, string? databaseName = null)
    {
        var connectionString = new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
        {
            Database = databaseName ?? $"eventum_users_{Guid.NewGuid():N}"
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        var context = new UsersDbContext(options);
        await context.Database.EnsureCreatedAsync(token);
        return context;
    }

    private static void SetId(User user, Guid id) =>
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, id);

    private sealed class ColumnInfo
    {
        public string ColumnName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string IsNullable { get; set; } = string.Empty;
        public int? MaxLength { get; set; }
    }
}
