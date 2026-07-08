using Eventum.Bookings.Application.Exceptions;
using Eventum.Bookings.Application.Interfaces;
using Eventum.Bookings.Application.Services;
using Eventum.Bookings.Domain;
using Eventum.Shared.Contracts;
using Microsoft.Extensions.Logging.Abstractions;

namespace Eventum.Bookings.Tests;

public class BookingServiceTests
{
    [Fact]
    public async Task CreateBookingAsync_ShouldCreatePendingBooking()
    {
        var token = TestContext.Current.CancellationToken;
        var repository = new InMemoryBookingRepository();
        var service = CreateService(repository);
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var booking = await service.CreateBookingAsync(eventId, userId, token: token);

        Assert.NotEqual(Guid.Empty, booking.Id);
        Assert.Equal(eventId, booking.EventId);
        Assert.Equal(userId, booking.UserId);
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Single(repository.Bookings);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldCreateUniqueBookings()
    {
        var token = TestContext.Current.CancellationToken;
        var repository = new InMemoryBookingRepository();
        var service = CreateService(repository);
        var userId = Guid.NewGuid();

        var first = await service.CreateBookingAsync(Guid.NewGuid(), userId, token: token);
        var second = await service.CreateBookingAsync(Guid.NewGuid(), userId, token: token);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldThrow_WhenActiveBookingLimitExceeded()
    {
        var token = TestContext.Current.CancellationToken;
        var repository = new InMemoryBookingRepository();
        var service = CreateService(repository);
        var userId = Guid.NewGuid();

        for (var i = 0; i < BookingConstants.MaxActiveBookingsPerUser; i++)
            await service.CreateBookingAsync(Guid.NewGuid(), userId, token: token);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            service.CreateBookingAsync(Guid.NewGuid(), userId, token: token));
    }

    [Fact]
    public async Task CancelBookingAsync_ShouldCancelOwnerBooking()
    {
        var token = TestContext.Current.CancellationToken;
        var repository = new InMemoryBookingRepository();
        var service = CreateService(repository);
        var userId = Guid.NewGuid();
        var booking = await service.CreateBookingAsync(Guid.NewGuid(), userId, token: token);

        await service.CancelBookingAsync(booking.Id, userId, token);

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    [Fact]
    public async Task CancelBookingAsync_ShouldThrow_WhenUserIsNotOwner()
    {
        var token = TestContext.Current.CancellationToken;
        var repository = new InMemoryBookingRepository();
        var service = CreateService(repository);
        var booking = await service.CreateBookingAsync(Guid.NewGuid(), Guid.NewGuid(), token: token);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CancelBookingAsync(booking.Id, Guid.NewGuid(), token));
    }

    [Fact]
    public async Task CancelBookingAsync_ShouldThrow_WhenBookingAlreadyCancelled()
    {
        var token = TestContext.Current.CancellationToken;
        var repository = new InMemoryBookingRepository();
        var service = CreateService(repository);
        var userId = Guid.NewGuid();
        var booking = await service.CreateBookingAsync(Guid.NewGuid(), userId, token: token);
        await service.CancelBookingAsync(booking.Id, userId, token);

        await Assert.ThrowsAsync<BookingAlreadyCancelledException>(() =>
            service.CancelBookingAsync(booking.Id, userId, token));
    }

    [Fact]
    public async Task GetBookingByIdAsync_ShouldThrow_WhenBookingIsMissing()
    {
        var token = TestContext.Current.CancellationToken;
        var service = CreateService(new InMemoryBookingRepository());

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.GetBookingByIdAsync(Guid.NewGuid(), token));
    }

    [Fact]
    public async Task ProcessBookingAsync_ShouldConfirmAndPublishBookingConfirmed()
    {
        var token = TestContext.Current.CancellationToken;
        var repository = new InMemoryBookingRepository();
        var publisher = new RecordingBookingEventPublisher();
        var service = CreateService(repository, publisher);
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var booking = await service.CreateBookingAsync(eventId, userId, token: token);

        await service.ProcessBookingAsync(booking.Id, token);

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
        Assert.Single(publisher.Messages);
        var message = publisher.Messages.Single();
        Assert.Equal(booking.Id, message.BookingId);
        Assert.Equal(eventId, message.EventId);
        Assert.Equal(userId, message.UserId);
        Assert.Equal(1, message.Seats);
    }

    [Fact]
    public async Task ProcessBookingAsync_ShouldDoNothing_WhenBookingWasCancelled()
    {
        var token = TestContext.Current.CancellationToken;
        var repository = new InMemoryBookingRepository();
        var publisher = new RecordingBookingEventPublisher();
        var service = CreateService(repository, publisher);
        var userId = Guid.NewGuid();
        var booking = await service.CreateBookingAsync(Guid.NewGuid(), userId, token: token);
        await service.CancelBookingAsync(booking.Id, userId, token);
        var processedAt = booking.ProcessedAt;

        await service.ProcessBookingAsync(booking.Id, token);

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal(processedAt, booking.ProcessedAt);
        Assert.Empty(publisher.Messages);
    }

    [Fact]
    public async Task ProcessBookingAsync_ShouldKeepConfirmedBooking_WhenPublishFails()
    {
        var token = TestContext.Current.CancellationToken;
        var repository = new InMemoryBookingRepository();
        var service = new BookingService(
            repository,
            new ThrowingBookingEventPublisher(),
            NullLogger<BookingService>.Instance);
        var booking = await service.CreateBookingAsync(Guid.NewGuid(), Guid.NewGuid(), token: token);

        await service.ProcessBookingAsync(booking.Id, token);

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    private static BookingService CreateService(
        InMemoryBookingRepository repository,
        RecordingBookingEventPublisher? publisher = null)
    {
        return new BookingService(
            repository,
            publisher ?? new RecordingBookingEventPublisher(),
            NullLogger<BookingService>.Instance);
    }

    private sealed class InMemoryBookingRepository : IBookingRepository
    {
        public List<Booking> Bookings { get; } = [];

        public Task<Booking?> GetByIdAsync(Guid id, CancellationToken token = default) =>
            Task.FromResult(Bookings.FirstOrDefault(booking => booking.Id == id));

        public Task<IReadOnlyCollection<Guid>> GetPendingBookingIdsAsync(CancellationToken token = default)
        {
            IReadOnlyCollection<Guid> ids = Bookings
                .Where(booking => booking.Status == BookingStatus.Pending)
                .Select(booking => booking.Id)
                .ToList();

            return Task.FromResult(ids);
        }

        public Task<int> GetActiveBookingCountByUserAsync(Guid userId, CancellationToken token = default)
        {
            var count = Bookings.Count(booking =>
                booking.UserId == userId &&
                (booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed));

            return Task.FromResult(count);
        }

        public Task AddAsync(Booking booking, CancellationToken token = default)
        {
            Bookings.Add(booking);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken token = default) => Task.CompletedTask;
    }

    private sealed class RecordingBookingEventPublisher : IBookingEventPublisher
    {
        public List<BookingConfirmed> Messages { get; } = [];

        public Task PublishAsync(BookingConfirmed message, CancellationToken token = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingBookingEventPublisher : IBookingEventPublisher
    {
        public Task PublishAsync(BookingConfirmed message, CancellationToken token = default) =>
            throw new InvalidOperationException("Broker is unavailable");
    }
}
