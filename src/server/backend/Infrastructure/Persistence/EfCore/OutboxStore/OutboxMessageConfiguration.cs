using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TicketFlow.Infrastructure.Persistence.EfCore.OutboxStore;

/// <summary>
/// Configuración de EF Core para la entidad OutboxMessage
/// </summary>
public class OutboxMessageConfiguration : IEntityTypeConfiguration<Outbox.OutboxMessage>
{
    public void Configure(EntityTypeBuilder<Outbox.OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        // Primary Key
        builder.HasKey(x => x.Id);

        // Properties
        builder.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.PayloadJson)
            .IsRequired()
            .HasColumnType("text"); // Para JSON grandes

        builder.Property(x => x.OccurredAt)
            .IsRequired();

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(100);

        builder.Property(x => x.DispatchedAt)
            .IsRequired(false);

        builder.Property(x => x.Attempts)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.Error)
            .HasMaxLength(2000);

        // Índices para optimizar consultas
        
        // Índice compuesto para buscar mensajes pendientes
        builder.HasIndex(x => new { x.DispatchedAt, x.OccurredAt })
            .HasDatabaseName("IX_OutboxMessages_Pending")
            // Postgres syntax (double quotes for case-sensitive identifiers)
            .HasFilter("\"DispatchedAt\" IS NULL"); // Solo mensajes sin despachar

        // Índice para búsqueda por tipo
        builder.HasIndex(x => x.Type)
            .HasDatabaseName("IX_OutboxMessages_Type");

        // Índice para rastreo de correlación
        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("IX_OutboxMessages_CorrelationId")
            .HasFilter("\"CorrelationId\" IS NOT NULL");

        // Índice para mensajes despachados (consultas de auditoría)
        builder.HasIndex(x => x.DispatchedAt)
            .HasDatabaseName("IX_OutboxMessages_DispatchedAt")
            .HasFilter("\"DispatchedAt\" IS NOT NULL");
    }
}
