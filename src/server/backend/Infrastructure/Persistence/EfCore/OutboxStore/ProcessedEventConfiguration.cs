using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TicketFlow.Infrastructure.Persistence.EfCore.OutboxStore;

/// <summary>
/// Configuración de EF Core para la entidad ProcessedEvent
/// </summary>
public class ProcessedEventConfiguration : IEntityTypeConfiguration<Outbox.ProcessedEvent>
{
    public void Configure(EntityTypeBuilder<Outbox.ProcessedEvent> builder)
    {
        builder.ToTable("ProcessedEvents");

        // Primary Key
        builder.HasKey(x => x.EventId);

        // Properties
        builder.Property(x => x.EventId)
            .IsRequired()
            .ValueGeneratedNever(); // El ID viene del evento de dominio, no se genera automáticamente

        builder.Property(x => x.ProcessedAt)
            .IsRequired();

        // Índices

        // Índice por fecha de procesamiento (para limpieza/auditoría)
        builder.HasIndex(x => x.ProcessedAt)
            .HasDatabaseName("IX_ProcessedEvents_ProcessedAt");
    }
}
