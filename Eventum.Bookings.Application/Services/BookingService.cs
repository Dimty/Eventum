using Eventum.Bookings.Application.Exceptions;
using Eventum.Bookings.Application.Interfaces;
using Eventum.Bookings.Domain;
using Eventum.Shared.Contracts;
using Microsoft.Extensions.Logging;
using UnauthorizedAccessException = System.UnauthorizedAccessException;

namespace Eventum.Bookings.Application.Services;

public class BookingService(
    IBookingRepository bookingRepository,
    IBookingEventPublisher bookingEventPublisher,
    ILogger<BookingService> logger) : IBookingService, IBookingProcessingService
{
    private const int MinDelay = 1000;
    private const int MaxDelay = 5000;

    public async Task<Booking> CreateBookingAsync(Guid eventId, Guid userId, int seats = 1, CancellationToken token = default)
    {
        var activeBookings = await bookingRepository.GetActiveBookingCountByUserAsync(userId, token);
        if (activeBookings >= BookingConstants.MaxActiveBookingsPerUser)
            throw new BusinessRuleViolationException(
                "BookingLimitExceeded",
                $"User '{userId}' has {activeBookings} active bookings");

        var booking = new Booking(eventId, userId, seats);
        await bookingRepository.AddAsync(booking, token);
        await bookingRepository.SaveChangesAsync(token);
        return booking;
    }

    public async Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken token = default)
    {
        return await bookingRepository.GetByIdAsync(bookingId, token)
            ?? throw new ResourceNotFoundException(nameof(Booking), bookingId);
    }

    public async Task CancelBookingAsync(Guid bookingId, Guid userId, CancellationToken token = default)
    {
        var booking = await GetBookingByIdAsync(bookingId, token);
        if (booking.UserId != userId)
            throw new UnauthorizedAccessException("Only booking owner can cancel booking");

        booking.Cancel();
        await bookingRepository.SaveChangesAsync(token);
    }

    public Task<IReadOnlyCollection<Guid>> GetPendingBookingIdsAsync(CancellationToken token = default) =>
        bookingRepository.GetPendingBookingIdsAsync(token);

    public async Task ProcessBookingAsync(Guid bookingId, CancellationToken token = default)
    {
        await Task.Delay(Random.Shared.Next(MinDelay, MaxDelay), token);

        var booking = await GetBookingByIdAsync(bookingId, token);
        if (booking.Status != BookingStatus.Pending)
            return;

        booking.Confirm();
        await bookingRepository.SaveChangesAsync(token);

        var message = new BookingConfirmed(
            booking.Id,
            booking.EventId,
            booking.UserId,
            booking.Seats,
            booking.ProcessedAt ?? DateTime.UtcNow);

        try
        {
            await bookingEventPublisher.PublishAsync(message, token);
            logger.LogInformation("BookingConfirmed published. BookingId={BookingId}, EventId={EventId}", booking.Id, booking.EventId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Booking was confirmed but BookingConfirmed was not published. BookingId={BookingId}", booking.Id);
        }
    }
}
