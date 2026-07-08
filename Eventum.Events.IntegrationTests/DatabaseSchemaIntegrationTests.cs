using Eventum.Events.Domain;
using Eventum.Events.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Eventum.Events.IntegrationTests;

public class DatabaseSchemaIntegrationTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task Database_ShouldHaveEventsTable_WithExpectedColumns()
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
                WHERE table_schema = 'public' AND table_name = 'events'
                ORDER BY ordinal_position
                """)
            .ToListAsync(token);

        Assert.NotEmpty(columns);
        Assert.Contains(columns, column => column.ColumnName == "id" && column.DataType == "uuid" && column.IsNullable == "NO");
        Assert.Contains(columns, column => column.ColumnName == "title" && column.DataType == "character varying" && column.MaxLength == 200);
        Assert.Contains(columns, column => column.ColumnName == "description" && column.DataType == "character varying" && column.MaxLength == 2000 && column.IsNullable == "YES");
        Assert.Contains(columns, column => column.ColumnName == "start_at" && column.DataType.Contains("timestamp", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(columns, column => column.ColumnName == "end_at" && column.DataType.Contains("timestamp", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(columns, column => column.ColumnName == "total_seats" && column.DataType == "integer");
        Assert.Contains(columns, column => column.ColumnName == "available_seats" && column.DataType == "integer");
    }

    [Fact]
    public async Task Database_ShouldNotCreateForeignKeys_ForEventsTable()
    {
        var token = TestContext.Current.CancellationToken;
        await using var context = await CreateContextAsync(token);

        var foreignKeys = await context.Database
            .SqlQuery<string>($"""
                SELECT constraint_name
                FROM information_schema.table_constraints
                WHERE table_schema = 'public'
                  AND table_name = 'events'
                  AND constraint_type = 'FOREIGN KEY'
                """)
            .ToListAsync(token);

        Assert.Empty(foreignKeys);
    }

    [Fact]
    public async Task Database_ShouldEnforcePrimaryKeyUniqueness_ForEvents()
    {
        var token = TestContext.Current.CancellationToken;
        var databaseName = $"eventum_events_{Guid.NewGuid():N}";
        await using var context = await CreateContextAsync(token, databaseName);
        var existing = CreateEvent("Event 1", DateTime.UtcNow.AddDays(1));
        var duplicate = CreateEvent("Event 2", DateTime.UtcNow.AddDays(2));
        var id = Guid.NewGuid();
        SetId(existing, id);
        SetId(duplicate, id);

        context.Events.Add(existing);
        await context.SaveChangesAsync(token);

        await using var newContext = await CreateContextAsync(token, databaseName);
        newContext.Events.Add(duplicate);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
            newContext.SaveChangesAsync(token));

        Assert.Equal("23505", Assert.IsType<PostgresException>(exception.InnerException).SqlState);
    }

    private static Event CreateEvent(string title, DateTime startAt) =>
        Event.Create(title, "Description", startAt, startAt.AddHours(2), 10);

    private async Task<EventsDbContext> CreateContextAsync(CancellationToken token, string? databaseName = null)
    {
        var connectionString = new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
        {
            Database = databaseName ?? $"eventum_events_{Guid.NewGuid():N}"
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<EventsDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        var context = new EventsDbContext(options);
        await context.Database.EnsureCreatedAsync(token);
        return context;
    }

    private static void SetId(Event ev, Guid id) =>
        typeof(Event).GetProperty(nameof(Event.Id))!.SetValue(ev, id);

    private sealed class ColumnInfo
    {
        public string ColumnName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string IsNullable { get; set; } = string.Empty;
        public int? MaxLength { get; set; }
    }
}
