using Eventum.Bookings.Domain;
using Microsoft.EntityFrameworkCore;

namespace Eventum.Bookings.Infrastructure.Data;

public class BookingsDbContext(DbContextOptions<BookingsDbContext> options) : DbContext(options)
{
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingsDbContext).Assembly);
    }
}
