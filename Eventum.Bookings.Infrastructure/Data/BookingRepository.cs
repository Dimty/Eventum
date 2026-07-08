using Eventum.Bookings.Application.Interfaces;
using Eventum.Bookings.Domain;
using Microsoft.EntityFrameworkCore;

namespace Eventum.Bookings.Infrastructure.Data;

public class BookingRepository(BookingsDbContext context) : IBookingRepository
{
    public Task<Booking?> GetByIdAsync(Guid id, CancellationToken token = default) =>
        context.Bookings.FirstOrDefaultAsync(booking => booking.Id == id, token);

    public async Task<IReadOnlyCollection<Guid>> GetPendingBookingIdsAsync(CancellationToken token = default)
    {
        return await context.Bookings
            .Where(booking => booking.Status == BookingStatus.Pending)
            .Select(booking => booking.Id)
            .ToListAsync(token);
    }

    public Task<int> GetActiveBookingCountByUserAsync(Guid userId, CancellationToken token = default) =>
        context.Bookings
            .Where(booking => booking.UserId == userId)
            .Where(booking => booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed)
            .CountAsync(token);

    public async Task AddAsync(Booking booking, CancellationToken token = default) =>
        await context.Bookings.AddAsync(booking, token);

    public Task SaveChangesAsync(CancellationToken token = default) =>
        context.SaveChangesAsync(token);
}
