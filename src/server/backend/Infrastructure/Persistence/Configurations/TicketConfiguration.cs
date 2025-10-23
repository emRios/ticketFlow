using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketFlow.Domain.Entities;

namespace TicketFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración de Entity Framework para la entidad Ticket
/// </summary>
public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        // Mapear a tabla "tickets" en minúsculas (PostgreSQL standard)
        builder.ToTable("tickets");

        // Configurar clave primaria
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasColumnName("id")
            .IsRequired();

        // Configurar propiedades
        builder.Property(t => t.Title)
            .HasColumnName("title")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasColumnName("description");

        builder.Property(t => t.CreatedAt)
            .HasColumnName("createdat")
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updatedat");

        builder.Property(t => t.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.Priority)
            .HasColumnName("priority")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.AssignedTo)
            .HasColumnName("assignedto")
            .HasMaxLength(100)
            .IsRequired(false);

        // Columna Version para control optimista de concurrencia
        builder.Property(t => t.Version)
            .HasColumnName("version")
            .IsRequired()
            .HasDefaultValue(0);

        // Ignorar eventos de dominio (no se persisten)
        builder.Ignore(t => t.DomainEvents);
    }
}
