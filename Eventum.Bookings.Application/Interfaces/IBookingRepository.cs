using Eventum.Bookings.Domain;

namespace Eventum.Bookings.Application.Interfaces;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken token = default);

    Task<IReadOnlyCollection<Guid>> GetPendingBookingIdsAsync(CancellationToken token = default);

    Task<int> GetActiveBookingCountByUserAsync(Guid userId, CancellationToken token = default);

    Task AddAsync(Booking booking, CancellationToken token = default);

    Task SaveChangesAsync(CancellationToken token = default);
}
