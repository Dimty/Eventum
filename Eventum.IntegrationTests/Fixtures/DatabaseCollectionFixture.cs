using Testcontainers.PostgreSql;

namespace Eventum.IntegrationTests.Fixtures;

public class DatabaseCollectionFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = 
        new PostgreSqlBuilder("postgres:16-alpine").Build();

    public string ConnectionString { get; private set; }

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        ConnectionString = _postgres.GetConnectionString();
    }

    public async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
}