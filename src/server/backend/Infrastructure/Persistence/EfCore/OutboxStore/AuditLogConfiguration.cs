using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketFlow.Infrastructure.Outbox;

namespace TicketFlow.Infrastructure.Persistence.EfCore.OutboxStore;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedNever();

        builder.Property(a => a.EntityType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.EntityId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.PerformedBy)
            .HasMaxLength(200);

        builder.Property(a => a.PerformedByName)
            .HasMaxLength(200);

        builder.Property(a => a.OccurredAt)
            .IsRequired();

        builder.Property(a => a.CorrelationId)
            .HasMaxLength(100);

        builder.Property(a => a.Data)
            .HasColumnType("text");

        builder.HasIndex(a => new { a.EntityType, a.EntityId });
        builder.HasIndex(a => a.OccurredAt);
        builder.HasIndex(a => a.CorrelationId);
    }
}
