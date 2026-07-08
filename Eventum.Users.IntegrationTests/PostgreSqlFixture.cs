using Testcontainers.PostgreSql;

namespace Eventum.Users.IntegrationTests;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public string ConnectionString => _postgres.GetConnectionString();

    public async ValueTask InitializeAsync() =>
        await _postgres.StartAsync();

    public async ValueTask DisposeAsync() =>
        await _postgres.DisposeAsync();
}
