namespace Eventum.Application.Interfaces.Services;

public interface IBookingProcessingService
{
    Task ProcessBookingAsync(Guid booking, CancellationToken token);
    Task<IEnumerable<Guid>> GetPendingBookingIdsAsync();
}