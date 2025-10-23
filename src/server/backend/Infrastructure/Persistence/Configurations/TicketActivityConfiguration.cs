using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketFlow.Domain.Entities;

namespace TicketFlow.Infrastructure.Persistence.Configurations;

public class TicketActivityConfiguration : IEntityTypeConfiguration<TicketActivity>
{
    public void Configure(EntityTypeBuilder<TicketActivity> builder)
    {
        builder.ToTable("TicketActivities");
        
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .ValueGeneratedNever();
        
        builder.Property(a => a.TicketId)
            .IsRequired();
        
        builder.Property(a => a.Action)
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(a => a.OccurredAt)
            .IsRequired();
        
        builder.Property(a => a.PerformedBy)
            .HasMaxLength(200);
        
        builder.Property(a => a.PerformedByName)
            .HasMaxLength(200);
        
        builder.Property(a => a.CorrelationId)
            .HasMaxLength(100);
        
        // Campos opcionales por tipo de acción
        builder.Property(a => a.Title)
            .HasMaxLength(500);
        
        builder.Property(a => a.AssigneeId)
            .HasMaxLength(200);
        
        builder.Property(a => a.AssigneeName)
            .HasMaxLength(200);
        
        builder.Property(a => a.Reason)
            .HasMaxLength(500);
        
        builder.Property(a => a.OldStatus)
            .HasMaxLength(50);
        
        builder.Property(a => a.NewStatus)
            .HasMaxLength(50);
        
        builder.Property(a => a.Comment)
            .HasMaxLength(1000);
        
        // Índices para consultas eficientes
        builder.HasIndex(a => a.TicketId);
        builder.HasIndex(a => a.OccurredAt);
        builder.HasIndex(a => a.CorrelationId);
        builder.HasIndex(a => new { a.TicketId, a.OccurredAt });
    }
}
