using Eventum.Application.Common;
using Eventum.Application.Exceptions;
using Eventum.Application.Interfaces.Repositories;
using Eventum.Application.Interfaces.Services;
using Eventum.Domain.Constants;
using Eventum.Domain.Enums;
using Eventum.Domain.Exceptions;
using Eventum.Domain.Models;
using UnauthorizedAccessException = System.UnauthorizedAccessException;

namespace Eventum.Application.Services;

public class BookingService(
    IBookingRepository bookingRepository,
    IEventRepository eventRepository,
    IAppLogger<BookingService> logger)
    : IBookingService, IBookingProcessingService
{
    private static readonly SemaphoreSlim ProcessingSemaphore = new(1, 1);

    private const int MinDelay = 1000;
    private const int MaxDelay = 5000;

    public async Task<Booking> CreateBookingAsync(Guid eventId, Guid userId)
    {
        await ProcessingSemaphore.WaitAsync();

        try
        {
            var ev = await eventRepository.GetByIdAsync(eventId);

            if (ev == null)
                throw new EntityNotFoundException(nameof(Event), eventId);

            if (ev.StartAt < DateTime.UtcNow)
                throw new PastEventBookingException(ev.Id.ToString(), ev.StartAt);
            
            if (!ev.TryReserveSeats())
                throw new NoAvailableSeatsException(ev.Id);

            var activeBookingCount = await bookingRepository.GetActiveBookingCountByUserAsync(userId);

            if (activeBookingCount >= BookingConstants.MaxActiveBookingPerUser)
                throw new BookingLimitExceededException(userId.ToString(), activeBookingCount, BookingConstants.MaxActiveBookingPerUser);
            
            var booking = new Booking(ev.Id, userId);

            await bookingRepository.AddAsync(booking);
            await bookingRepository.SaveChangesAsync();

            return booking;
        }
        catch (EntityNotFoundException)
        {
            throw new ResourceNotFoundException(nameof(Event), eventId);
        }
        catch(NoAvailableSeatsException ex)
        {
            throw new BusinessRuleViolationException("No available seats for user", ex.Message);
        }
        catch(BookingLimitExceededException ex)
        {
            throw new BusinessRuleViolationException("No available seats", ex.Message);
        }
        finally
        {
            ProcessingSemaphore.Release();
        }
    }

    public async Task<Booking> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = await bookingRepository.GetByIdAsync(bookingId);

        return booking ?? throw new ResourceNotFoundException(nameof(Booking), bookingId);
    }
    
    public async Task<IEnumerable<Guid>> GetPendingBookingIdsAsync()
    {
        return await bookingRepository.FindWithProjectionAsync(
            bk => bk.Status == BookingStatus.Pending, bk => bk.Id );
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
                var ev = await eventRepository.GetByIdAsync(booking.EventId, token);

                if (ev == null)
                    throw new EntityNotFoundException(nameof(Event), booking.EventId);

                booking.Confirm();

                await bookingRepository.SaveChangesAsync(token);

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
                var ev = await eventRepository.GetByIdAsync(booking.EventId, token);

                if (ev == null)
                    throw new ResourceNotFoundException(nameof(Event), booking.EventId);

                booking.Reject();

                await bookingRepository.SaveChangesAsync(token);

                logger.LogWarning("Operation canceled for booking {BookingId}", booking.Id);
            }
            finally
            {
                ProcessingSemaphore.Release();
            }
        }
        catch (EntityNotFoundException)
        {
            await ProcessingSemaphore.WaitAsync(CancellationToken.None);
            try
            {
                var booking = await GetBookingByIdAsync(bookingId);

                booking.Reject();

                await bookingRepository.SaveChangesAsync(token);

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
                var ev = await eventRepository.GetByIdAsync(booking.EventId, token);

                if (ev == null)
                    throw new ResourceNotFoundException(nameof(Event), booking.EventId);

                ev.ReleaseSeats();
                booking.Reject();

                await bookingRepository.SaveChangesAsync(token);

                logger.LogError(ex, "Error processing booking {BookingId}", booking.Id);
            }
            finally
            {
                ProcessingSemaphore.Release();
            }
        }
    }
}