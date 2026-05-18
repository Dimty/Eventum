using System.Linq.Expressions;
using Eventum.Data.Interfaces;
using Eventum.DataAccess.Contexts;
using Eventum.Models;
using Microsoft.EntityFrameworkCore;

namespace Eventum.Data.Repositories;

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

    public async Task AddAsync(Booking ev, CancellationToken token = default)=>
        await context.Bookings.AddAsync(ev, token);

    public async Task SaveChangesAsync(CancellationToken token = default)=>
        await context.SaveChangesAsync(token);
    
}