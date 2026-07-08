using Eventum.Users.Domain;

namespace Eventum.Users.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByLoginAsync(string login, CancellationToken token = default);

    Task AddAsync(User user, CancellationToken token = default);

    Task SaveChangesAsync(CancellationToken token = default);
}
