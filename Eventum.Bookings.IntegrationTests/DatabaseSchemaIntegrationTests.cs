using Eventum.Bookings.Domain;
using Eventum.Bookings.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Eventum.Bookings.IntegrationTests;

public class DatabaseSchemaIntegrationTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task Database_ShouldHaveBookingsTable_WithExpectedColumns()
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
                WHERE table_schema = 'public' AND table_name = 'bookings'
                ORDER BY ordinal_position
                """)
            .ToListAsync(token);

        Assert.NotEmpty(columns);
        Assert.Contains(columns, column => column.ColumnName == "id" && column.DataType == "uuid" && column.IsNullable == "NO");
        Assert.Contains(columns, column => column.ColumnName == "event_id" && column.DataType == "uuid" && column.IsNullable == "NO");
        Assert.Contains(columns, column => column.ColumnName == "user_id" && column.DataType == "uuid" && column.IsNullable == "NO");
        Assert.Contains(columns, column => column.ColumnName == "seats" && column.DataType == "integer" && column.IsNullable == "NO");
        Assert.Contains(columns, column => column.ColumnName == "status" && column.DataType == "character varying" && column.MaxLength == 20);
        Assert.Contains(columns, column => column.ColumnName == "created_at" && column.DataType.Contains("timestamp", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(columns, column => column.ColumnName == "processed_at" && column.DataType.Contains("timestamp", StringComparison.OrdinalIgnoreCase) && column.IsNullable == "YES");
    }

    [Fact]
    public async Task Database_ShouldHaveIndexes_ForUserIdAndEventId()
    {
        var token = TestContext.Current.CancellationToken;
        await using var context = await CreateContextAsync(token);

        var indexes = await context.Database
            .SqlQuery<string>($"""
                SELECT indexdef
                FROM pg_indexes
                WHERE schemaname = 'public' AND tablename = 'bookings'
                """)
            .ToListAsync(token);

        Assert.Contains(indexes, index => index.Contains("(user_id)", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(indexes, index => index.Contains("(event_id)", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Database_ShouldNotCreateForeignKeys_ForBookingsTable()
    {
        var token = TestContext.Current.CancellationToken;
        await using var context = await CreateContextAsync(token);

        var foreignKeys = await context.Database
            .SqlQuery<string>($"""
                SELECT constraint_name
                FROM information_schema.table_constraints
                WHERE table_schema = 'public'
                  AND table_name = 'bookings'
                  AND constraint_type = 'FOREIGN KEY'
                """)
            .ToListAsync(token);

        Assert.Empty(foreignKeys);
    }

    [Fact]
    public async Task Database_ShouldEnforcePrimaryKeyUniqueness_ForBookings()
    {
        var token = TestContext.Current.CancellationToken;
        var databaseName = $"eventum_bookings_{Guid.NewGuid():N}";
        await using var context = await CreateContextAsync(token, databaseName);
        var existing = new Booking(Guid.NewGuid(), Guid.NewGuid());
        var duplicate = new Booking(Guid.NewGuid(), Guid.NewGuid());
        var id = Guid.NewGuid();
        SetId(existing, id);
        SetId(duplicate, id);

        context.Bookings.Add(existing);
        await context.SaveChangesAsync(token);

        await using var newContext = await CreateContextAsync(token, databaseName);
        newContext.Bookings.Add(duplicate);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
            newContext.SaveChangesAsync(token));

        Assert.Equal("23505", Assert.IsType<PostgresException>(exception.InnerException).SqlState);
    }

    private async Task<BookingsDbContext> CreateContextAsync(CancellationToken token, string? databaseName = null)
    {
        var connectionString = new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
        {
            Database = databaseName ?? $"eventum_bookings_{Guid.NewGuid():N}"
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<BookingsDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        var context = new BookingsDbContext(options);
        await context.Database.EnsureCreatedAsync(token);
        return context;
    }

    private static void SetId(Booking booking, Guid id) =>
        typeof(Booking).GetProperty(nameof(Booking.Id))!.SetValue(booking, id);

    private sealed class ColumnInfo
    {
        public string ColumnName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string IsNullable { get; set; } = string.Empty;
        public int? MaxLength { get; set; }
    }
}
