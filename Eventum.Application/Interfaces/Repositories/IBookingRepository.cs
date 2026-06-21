using System.Linq.Expressions;
using Eventum.Domain.Models;

namespace Eventum.Application.Interfaces.Repositories;

public interface IBookingRepository
{
    Task<IEnumerable<TProjection>> FindWithProjectionAsync<TProjection>(
        Expression<Func<Booking, bool>> predicate, 
        Expression<Func<Booking, TProjection>> projection, 
        CancellationToken token = default);
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken token = default);
    Task AddAsync(Booking ev, CancellationToken token = default);
    
    Task<int> GetActiveBookingCountByUserAsync(Guid userId, CancellationToken token = default);
    
    Task<bool> DeleteAsync(Booking booking, CancellationToken token = default);
    Task SaveChangesAsync(CancellationToken token = default);
}