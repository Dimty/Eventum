using Eventum.Bookings.Domain;
using Eventum.Bookings.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Eventum.Bookings.IntegrationTests;

public class BookingRepositoryIntegrationTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task AddAsync_ShouldPersistBooking()
    {
        var token = TestContext.Current.CancellationToken;
        await using var context = await CreateContextAsync(token);
        var repository = new BookingRepository(context);
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());

        await repository.AddAsync(booking, token);
        await repository.SaveChangesAsync(token);

        context.ChangeTracker.Clear();
        var saved = await repository.GetByIdAsync(booking.Id, token);

        Assert.NotNull(saved);
        Assert.Equal(booking.EventId, saved.EventId);
        Assert.Equal(BookingStatus.Pending, saved.Status);
    }

    [Fact]
    public async Task GetPendingBookingIdsAsync_ShouldReturnOnlyPendingBookings()
    {
        var token = TestContext.Current.CancellationToken;
        await using var context = await CreateContextAsync(token);
        var repository = new BookingRepository(context);
        var pending = new Booking(Guid.NewGuid(), Guid.NewGuid());
        var confirmed = new Booking(Guid.NewGuid(), Guid.NewGuid());
        confirmed.Confirm();
        await repository.AddAsync(pending, token);
        await repository.AddAsync(confirmed, token);
        await repository.SaveChangesAsync(token);

        var ids = await repository.GetPendingBookingIdsAsync(token);

        Assert.Single(ids);
        Assert.Equal(pending.Id, ids.Single());
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenBookingDoesNotExist()
    {
        var token = TestContext.Current.CancellationToken;
        await using var context = await CreateContextAsync(token);
        var repository = new BookingRepository(context);

        var result = await repository.GetByIdAsync(Guid.NewGuid(), token);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetActiveBookingCountByUserAsync_ShouldCountPendingAndConfirmed()
    {
        var token = TestContext.Current.CancellationToken;
        await using var context = await CreateContextAsync(token);
        var repository = new BookingRepository(context);
        var userId = Guid.NewGuid();
        var pending = new Booking(Guid.NewGuid(), userId);
        var confirmed = new Booking(Guid.NewGuid(), userId);
        confirmed.Confirm();
        var cancelled = new Booking(Guid.NewGuid(), userId);
        cancelled.Cancel();

        await repository.AddAsync(pending, token);
        await repository.AddAsync(confirmed, token);
        await repository.AddAsync(cancelled, token);
        await repository.SaveChangesAsync(token);

        var count = await repository.GetActiveBookingCountByUserAsync(userId, token);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetActiveBookingCountByUserAsync_ShouldExcludeRejectedBookings()
    {
        var token = TestContext.Current.CancellationToken;
        await using var context = await CreateContextAsync(token);
        var repository = new BookingRepository(context);
        var userId = Guid.NewGuid();
        var pending = new Booking(Guid.NewGuid(), userId);
        var rejected = new Booking(Guid.NewGuid(), userId);
        rejected.Reject();

        await repository.AddAsync(pending, token);
        await repository.AddAsync(rejected, token);
        await repository.SaveChangesAsync(token);

        var count = await repository.GetActiveBookingCountByUserAsync(userId, token);

        Assert.Equal(1, count);
    }

    private async Task<BookingsDbContext> CreateContextAsync(CancellationToken token)
    {
        var connectionString = new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
        {
            Database = $"eventum_bookings_{Guid.NewGuid():N}"
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<BookingsDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        var context = new BookingsDbContext(options);
        await context.Database.EnsureCreatedAsync(token);
        return context;
    }
}
