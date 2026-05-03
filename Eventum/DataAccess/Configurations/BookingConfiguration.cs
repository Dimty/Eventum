using Eventum.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eventum.DataAccess.Configurations;

public class BookingConfiguration:IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        
        builder.Property(x => x.Status)
            .HasConversion<string>();

        builder.HasOne(b => b.Event)
            .WithMany(b => b.Bookings)
            .HasForeignKey(b => b.EventId);
    }
}