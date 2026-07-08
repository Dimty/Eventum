using Eventum.Users.Application.Interfaces;
using Eventum.Users.Domain;
using Microsoft.EntityFrameworkCore;

namespace Eventum.Users.Infrastructure.Data;

public class UserRepository(UsersDbContext context) : IUserRepository
{
    public Task<User?> GetByLoginAsync(string login, CancellationToken token = default) =>
        context.Users.FirstOrDefaultAsync(user => user.Login == login, token);

    public async Task AddAsync(User user, CancellationToken token = default) =>
        await context.Users.AddAsync(user, token);

    public Task SaveChangesAsync(CancellationToken token = default) =>
        context.SaveChangesAsync(token);
}
