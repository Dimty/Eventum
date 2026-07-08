using Eventum.Events.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eventum.Events.Infrastructure.Data;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");

        builder.HasKey(ev => ev.Id);

        builder.Property(ev => ev.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(ev => ev.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(ev => ev.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        builder.Property(ev => ev.StartAt)
            .HasColumnName("start_at")
            .IsRequired();

        builder.Property(ev => ev.EndAt)
            .HasColumnName("end_at")
            .IsRequired();

        builder.Property(ev => ev.TotalSeats)
            .HasColumnName("total_seats")
            .IsRequired();

        builder.Property(ev => ev.AvailableSeats)
            .HasColumnName("available_seats")
            .IsRequired();
    }
}
