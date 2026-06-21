using Eventum.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Eventum.Infrastructure.Data.Contexts;

public class AppDbContext : DbContext
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public  DbSet<User> Users => Set<User>();
    
    public AppDbContext(DbContextOptions<AppDbContext> options):base(options){}
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}