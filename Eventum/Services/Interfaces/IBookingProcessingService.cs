using Eventum.Models;

namespace Eventum.Services.Interfaces;

public interface IBookingProcessingService
{
    Task ProcessBookingAsync(Guid booking, CancellationToken token);
    Task<IEnumerable<Guid>> GetPendingBookingIdsAsync();
}