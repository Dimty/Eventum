using System.Linq.Expressions;
using Eventum.Application.Interfaces.Repositories;
using Eventum.Domain.Enums;
using Eventum.Domain.Models;
using Eventum.Infrastructure.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Eventum.Infrastructure.Data.Repositories;

public class BookingRepository(AppDbContext context) : IBookingRepository
{
    public async Task<IEnumerable<TProjection>> FindWithProjectionAsync<TProjection>(
        Expression<Func<Booking, bool>> predicate, Expression<Func<Booking, TProjection>> projection,
        CancellationToken token = default)
    {
        return await context.Bookings.Where(predicate).Select(projection).ToListAsync(token);
    }

    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken token = default) =>
        await context.Bookings.FirstOrDefaultAsync(booking => booking.Id == id, token);

    public async Task<Booking?> GetByIdWithUserAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        await context.Bookings
            .Include(b => b.User)
            .AsSplitQuery()
            .FirstOrDefaultAsync(booking => booking.Id == bookingId, cancellationToken);

    public async Task AddAsync(Booking ev, CancellationToken token = default) =>
        await context.Bookings.AddAsync(ev, token);

    public async Task<int> GetActiveBookingCountByUserAsync(Guid userId, CancellationToken token = default) =>
        await context.Bookings
            .CountAsync(b => b.UserId == userId
                             && b.Status == BookingStatus.Confirmed
                             || b.Status == BookingStatus.Pending, token);


    public async Task<bool> DeleteAsync(Booking booking, CancellationToken token = default)
    {
        context.Bookings.Remove(booking);
        await SaveChangesAsync(token);

        return true;
    }

    public async Task SaveChangesAsync(CancellationToken token = default) =>
        await context.SaveChangesAsync(token);
}