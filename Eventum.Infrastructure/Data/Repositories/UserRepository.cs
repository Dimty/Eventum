using Eventum.Application.Interfaces.Repositories;
using Eventum.Domain.Models;
using Eventum.Infrastructure.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Eventum.Infrastructure.Data.Repositories;

public class UserRepository(AppDbContext context):IUserRepository
{
    public async Task<User?> GetByLoginAsync(string login, CancellationToken token = default)=>
        await context.Users.FirstOrDefaultAsync(x=>x.Login == login, token);

    public async Task AddAsync(User user, CancellationToken token = default)=>
        await context.Users.AddAsync(user, token);

    public async Task SaveChangesAsync(CancellationToken token = default) =>
        await context.SaveChangesAsync(token);
}