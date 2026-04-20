using System.Collections.Concurrent;
using Eventum.DTO;
using Eventum.Exceptions;
using Eventum.Models;
using Eventum.Services.Interfaces;

namespace Eventum.Services;

public class BookingService(IEventService eventService) : IBookingService, IBookingProcessingService
{
    private readonly ConcurrentDictionary<Guid, Booking> _bookings = new();
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);
    private readonly IEventService _eventService = eventService;
    private readonly object _bookingLock = new();

    private readonly Random _random = new();

    public Task<Booking> CreateBookingAsync(Guid eventId)
    {
        lock (_bookingLock)
        {
            var ev = _eventService.GetById(eventId)!;

            if (!ev.TryReserveSeats())
                throw new NoAvailableSeatsException();

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _bookings[booking.Id] = booking;

            return Task.FromResult(booking);
        }
    }

    public Task<Booking> GetBookingByIdAsync(Guid bookingId)
    {
        if (!_bookings.TryGetValue(bookingId, out var booking))
            throw new NotFoundException($"Booking {bookingId} not found");

        return Task.FromResult(booking);
    }

    public IEnumerable<Booking> GetPendingBookings()
    {
        return _bookings.Values.Where(b => b.Status == BookingStatus.Pending).ToList();
    }

    public async Task ProcessBookingAsync(Booking booking, CancellationToken token)
    {
        try
        {
            await Task.Delay(Random.Shared.Next(1000, 5000), token);

            await _processingSemaphore.WaitAsync(token);

            try
            {
                _eventService.GetById(booking.EventId);
                booking.Confirm();
                _bookings[booking.Id] = booking;
            }
            finally
            {
                _processingSemaphore.Release();
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Operation canceled");
        }
        catch (Exception)
        {
            await _processingSemaphore.WaitAsync(token);

            try
            {
                var ev = _eventService.GetById(booking.EventId)!;

                booking.Reject();
                ev.ReleaseSeats();
                _eventService.Update(ev.Id, new UpdateEventDto
                {
                    Description =  ev.Description,
                    StartAt =  ev.StartAt,
                    EndAt = ev.EndAt,
                });
                _bookings[booking.Id] = booking;
            }
            finally
            {
                _processingSemaphore.Release();
            }
        }
    }
}