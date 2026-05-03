using System.Collections.Concurrent;
using Eventum.DataAccess.Contexts;
using Eventum.DTO;
using Eventum.Exceptions;
using Eventum.Models;
using Eventum.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Eventum.Services;

public class BookingService(AppDbContext context, ILogger<BookingService> logger)
    : IBookingService, IBookingProcessingService
{
    private static readonly SemaphoreSlim ProcessingSemaphore = new(1, 1);

    private const int MinDelay = 1000;
    private const int MaxDelay = 5000;

    public async Task<Booking> CreateBookingAsync(Guid eventId)
    {
        await ProcessingSemaphore.WaitAsync();

        try
        {
            var ev = context.Events.FirstOrDefault(ev => ev.Id == eventId);

            if (ev == null)
                throw new NotFoundException($"Event {eventId} not found");

            if (!ev.TryReserveSeats())
                throw new NoAvailableSeatsException();

            var booking = new Booking(ev.Id);

            await context.Bookings.AddAsync(booking);
            await context.SaveChangesAsync();

            return booking;
        }
        finally
        {
            ProcessingSemaphore.Release();
        }
    }

    public async Task<Booking> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = await context.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
            throw new NotFoundException($"Booking {bookingId} not found");

        return booking;
    }

    public async Task<IEnumerable<Guid>> GetPendingBookingIdsAsync()
    {
        return await context.Bookings.Where(b => b.Status == BookingStatus.Pending)
            .Select(b => b.Id).ToListAsync();
    }

    public async Task ProcessBookingAsync(Guid bookingId, CancellationToken token)
    {
        try
        {
            await Task.Delay(Random.Shared.Next(MinDelay, MaxDelay), token);

            await ProcessingSemaphore.WaitAsync(token);

            try
            {
                var booking = await GetBookingByIdAsync(bookingId);
                var ev = context.Events.FirstOrDefault(ev => ev.Id == booking.EventId);

                if (ev == null)
                    throw new NotFoundException($"Event {booking.EventId} not found");

                booking.Confirm();

                await context.SaveChangesAsync(token);

                logger.LogInformation("Booking {BookingId} confirmed for event {EventId}", booking.Id, booking.EventId);
            }
            finally
            {
                ProcessingSemaphore.Release();
            }
        }
        catch (OperationCanceledException)
        {
            await ProcessingSemaphore.WaitAsync(CancellationToken.None);
            try
            {
                var booking = await GetBookingByIdAsync(bookingId);
                var ev = context.Events.FirstOrDefault(ev => ev.Id == booking.EventId);

                if (ev == null)
                    throw new NotFoundException($"Event {booking.EventId} not found");

                booking.Reject();
                
                await context.SaveChangesAsync(token);

                logger.LogWarning("Operation canceled for booking {BookingId}", booking.Id);
            }
            finally
            {
                ProcessingSemaphore.Release();
            }
        }
        catch (NotFoundException)
        {
            await ProcessingSemaphore.WaitAsync(CancellationToken.None);
            try
            {
                var booking = await GetBookingByIdAsync(bookingId);

                booking.Reject();

                await context.SaveChangesAsync(token);
                
                logger.LogWarning("Event {EventId} was deleted for booking {BookingId}", booking.EventId, booking.Id);
            }
            finally
            {
                ProcessingSemaphore.Release();
            }
        }
        catch (Exception ex)
        {
            await ProcessingSemaphore.WaitAsync(CancellationToken.None);
            try
            {
                var booking = await GetBookingByIdAsync(bookingId);
                var ev = context.Events.FirstOrDefault(ev => ev.Id == booking.EventId);

                if (ev == null)
                    throw new NotFoundException($"Event {booking.EventId} not found");

                ev.ReleaseSeats();
                booking.Reject();
                
                await context.SaveChangesAsync(token);
                
                logger.LogError(ex, "Error processing booking {BookingId}", booking.Id);
            }
            finally
            {
                ProcessingSemaphore.Release();
            }
        }
    }
}