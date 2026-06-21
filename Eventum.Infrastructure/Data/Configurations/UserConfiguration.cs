using Eventum.Domain.Enums;
using Eventum.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eventum.Infrastructure.Data.Configurations;

public class UserConfiguration:IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.HasIndex(u => u.Login);
        
        builder.Property(u => u.Id)
            .HasColumnName("id")
            .IsRequired();
        
        builder.Property(u => u.Login)
            .HasColumnName("login")
            .IsRequired();
        
        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .IsRequired();

        builder.Property(u => u.Role)
            .HasColumnName("role")
            .IsRequired()
            .HasConversion<int>(); //TODO: mb create a dictionary for roles
    }
}