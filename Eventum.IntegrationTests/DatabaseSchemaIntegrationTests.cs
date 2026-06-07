using Eventum.IntegrationTests.Base;
using Eventum.IntegrationTests.Fixtures;
using Eventum.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Eventum.IntegrationTests;

[Collection("Database collection")]
public class DatabaseSchemaIntegrationTests(DatabaseCollectionFixture fixture) : DatabaseTestBase(fixture)
{
    [Fact]
    public async Task Database_ShouldHaveRequiredTables_AfterMigration()
    {
        // Arrange
        await using var context = CreateContext();

        // Act
        var tables = await context.Database
            .SqlQuery<string>($@"
                SELECT table_name 
                FROM information_schema.tables 
                WHERE table_schema = 'public' 
                AND table_type = 'BASE TABLE'
                AND table_name IN ('events', 'bookings')
                ORDER BY table_name")
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEmpty(tables);
        Assert.Contains("bookings", tables);
        Assert.Contains("events", tables);
        Assert.Equal(2, tables.Count);
    }

    [Fact]
    public async Task Database_ShouldHaveEventsTable_WithExpectedColumns()
    {
        // Arrange
        await using var context = CreateContext();

        // Act
        var columns = await context.Database
            .SqlQuery<ColumnInfo>($@"
                SELECT column_name as ColumnName, data_type as DataType, is_nullable as IsNullable
                FROM information_schema.columns 
                WHERE table_schema = 'public' 
                AND table_name = 'events'
                ORDER BY ordinal_position")
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEmpty(columns);

        var requiredColumns = new[] { "id", "title", "description", "start_at", "end_at", "total_seats" };
        foreach (var col in requiredColumns)
        {
            Assert.Contains(columns, c => c.ColumnName == col);
        }

        var idColumn = columns.First(c => c.ColumnName == "id");
        Assert.Contains("uuid", idColumn.DataType.ToLower());
    }

    [Fact]
    public async Task Database_ShouldHaveBookingsTable_WithExpectedColumns()
    {
        // Arrange
        await using var context = CreateContext();

        // Act
        var columns = await context.Database
            .SqlQuery<ColumnInfo>($@"
                SELECT column_name as ColumnName, data_type as DataType, is_nullable as IsNullable
                FROM information_schema.columns 
                WHERE table_schema = 'public' 
                AND table_name = 'bookings'
                ORDER BY ordinal_position")
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEmpty(columns);

        var requiredColumns = new[] { "id", "event_id", "status", "processed_at", "created_at" };
        foreach (var col in requiredColumns)
        {
            Assert.Contains(columns, c => c.ColumnName == col);
        }

        var eventIdColumn = columns.First(c => c.ColumnName == "event_id");
        Assert.Contains("uuid", eventIdColumn.DataType.ToLower());
        Assert.Equal("NO", eventIdColumn.IsNullable.ToUpper());
    }

    [Fact]
    public async Task Database_ShouldHaveForeignKeyConstraint_BetweenBookingsAndEvents()
    {
        // Arrange
        await using var context = CreateContext();

        // Act
        var constraints = await context.Database
            .SqlQuery<ConstraintInfo>($@"
                SELECT 
                    tc.constraint_name as ConstraintName,
                    tc.constraint_type as ConstraintType,
                    kcu.column_name as ColumnName,
                    ccu.table_name AS ReferencedTableName,
                    ccu.column_name AS ReferencedColumnName
                FROM information_schema.table_constraints tc
                JOIN information_schema.key_column_usage kcu
                    ON tc.constraint_name = kcu.constraint_name
                    AND tc.table_schema = kcu.table_schema
                JOIN information_schema.constraint_column_usage ccu
                    ON ccu.constraint_name = tc.constraint_name
                    AND ccu.table_schema = tc.table_schema
                WHERE tc.constraint_type = 'FOREIGN KEY' 
                    AND tc.table_name = 'bookings'
                    AND kcu.column_name = 'event_id'
                    AND ccu.table_name = 'events'")
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEmpty(constraints);
        Assert.Single(constraints);

        var fkConstraint = constraints[0];
        Assert.Equal("FOREIGN KEY", fkConstraint.ConstraintType);
        Assert.Equal("event_id", fkConstraint.ColumnName);
        Assert.Equal("events", fkConstraint.ReferencedTableName);
        Assert.Equal("id", fkConstraint.ReferencedColumnName);
    }

    [Fact]
    public async Task Database_EventTable_ShouldEnforcePrimaryKeyUniqueness_WhenInsertingDuplicateId()
    {
        // Arrange
        await ResetDatabaseAsync();

        var context = CreateContext();

        var specificId = Guid.NewGuid();
        var event1 = Event.Create(
            "Event with Unique ID",
            "First event with specific ID",
            DateTime.UtcNow.AddDays(10),
            DateTime.UtcNow.AddDays(10).AddHours(4),
            100
        );
        
        typeof(Event).GetProperty("Id")?.SetValue(event1, specificId);

        context.Events.Add(event1);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var event2 = Event.Create(
            "Duplicate Event",
            "This event has the same ID",
            DateTime.UtcNow.AddDays(20),
            DateTime.UtcNow.AddDays(20).AddHours(4),
            200
        );

        typeof(Event).GetProperty("Id")?.SetValue(event2, specificId);

        var newContext = CreateContext();
        newContext.Events.Add(event2);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await newContext.SaveChangesAsync(TestContext.Current.CancellationToken));

        var postgresException = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal("23505", postgresException.SqlState);
    }

    [Fact]
    public async Task Database_BookingTable_ShouldEnforcePrimaryKeyUniqueness_WhenInsertingDuplicateId()
    {
        // Arrange
        await ResetDatabaseAsync();

        var context = CreateContext();

        var specificId = Guid.NewGuid();
        var @event = Event.Create(
            "Event with Unique ID",
            "First event with specific ID",
            DateTime.UtcNow.AddDays(10),
            DateTime.UtcNow.AddDays(10).AddHours(4),
            100
        );
        context.Events.Add(@event);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        
        var bk1 = new Booking(@event.Id);
        typeof(Booking).GetProperty("Id")?.SetValue(bk1, specificId);

        context.Bookings.Add(bk1);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        
        var bk2 = new Booking(@event.Id);
        typeof(Booking).GetProperty("Id")?.SetValue(bk2, specificId);
        
        var newContext = CreateContext();
        newContext.Bookings.Add(bk2);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await newContext.SaveChangesAsync(TestContext.Current.CancellationToken));

        var postgresException = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal("23505", postgresException.SqlState);
    }
    
    [Fact]
    public async Task Database_ShouldSupportOneToManyRelationship_BetweenEventsAndBookings()
    {
        // Arrange
        await ResetDatabaseAsync();

        var context = CreateContext();

        var testEvent = Event.Create(
            "Conference 2024",
            "Big tech conference",
            DateTime.UtcNow.AddDays(30),
            DateTime.UtcNow.AddDays(30).AddHours(8),
            100
        );

        context.Events.Add(testEvent);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var bookings = new List<Booking>
        {
            new (testEvent.Id),
            new (testEvent.Id),
            new (testEvent.Id),
            new (testEvent.Id),
            new (testEvent.Id)
        };

        // Act
        context.Bookings.AddRange(bookings);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var newContext = CreateContext();
        var eventWithBookings = await newContext.Events
            .Include(e => e.Bookings)
            .FirstOrDefaultAsync(e => e.Id == testEvent.Id,
                TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(eventWithBookings);
        Assert.NotNull(eventWithBookings.Bookings);
        Assert.Equal(5, eventWithBookings.Bookings.Count);

        Assert.All(eventWithBookings.Bookings, booking =>
            Assert.Equal(testEvent.Id, booking.EventId));

        var bookingIds = eventWithBookings.Bookings.Select(b => b.Id).ToList();
        Assert.All(bookingIds, id => Assert.Contains(bookings, b => b.Id == id));
    }

    private class ColumnInfo
    {
        public string ColumnName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string IsNullable { get; set; } = string.Empty;
    }

    private class ConstraintInfo
    {
        public string ConstraintType { get; set; } = string.Empty;
        public string ColumnName { get; set; } = string.Empty;
        public string ReferencedTableName { get; set; } = string.Empty;
        public string ReferencedColumnName { get; set; } = string.Empty;
    }
}