using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketFlow.Domain.Entities;

namespace TicketFlow.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .HasColumnName("id");

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.Role)
            .HasColumnName("role")
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("AGENT");

        builder.Property(u => u.IsActive)
            .HasColumnName("isactive")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(u => u.LastAssignedAt)
            .HasColumnName("lastassignedat")
            .IsRequired(false);

        // Índice compuesto para query de ranking
        builder.HasIndex(u => new { u.Role, u.IsActive, u.LastAssignedAt })
            .HasDatabaseName("ix_users_role_isactive_lastassignedat");
    }
}
