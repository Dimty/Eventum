namespace Eventum.Bookings.Domain;

public class BookingAlreadyCancelledException(Guid bookingId)
    : Exception($"Booking '{bookingId}' is already cancelled");
