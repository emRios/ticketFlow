using System.Text.Json;
using TicketFlow.Infrastructure.Outbox;

namespace TicketFlow.Api.DTOs;

public record TicketActivityItemDto(
    string Action,
    DateTime OccurredAt,
    string? PerformedBy,
    string? PerformedByName,
    string? CorrelationId,
    // Datos normalizados por acción
    string? Title,
    string? AssigneeId,
    string? Reason,
    string? OldStatus,
    string? NewStatus,
    string? Comment
)
{
    public static TicketActivityItemDto FromAuditLog(AuditLog a)
    {
        string? title = null;
        string? assigneeId = null;
        string? reason = null;
        string? oldStatus = null;
        string? newStatus = null;
        string? comment = null;

        if (!string.IsNullOrWhiteSpace(a.Data))
        {
            try
            {
                using var doc = JsonDocument.Parse(a.Data!);
                var root = doc.RootElement;
                switch (a.Action)
                {
                    case "TicketCreated":
                        title = root.TryGetProperty("Title", out var t) ? t.GetString() : null;
                        break;
                    case "TicketAssigned":
                        assigneeId = root.TryGetProperty("AssigneeId", out var asg) ? asg.GetString() : null;
                        reason = root.TryGetProperty("Reason", out var r) ? r.GetString() : null;
                        break;
                    case "TicketStatusChanged":
                        oldStatus = root.TryGetProperty("OldStatus", out var os) ? os.GetString() : null;
                        newStatus = root.TryGetProperty("NewStatus", out var ns) ? ns.GetString() : null;
                        comment = root.TryGetProperty("Comment", out var c) ? c.GetString() : null;
                        break;
                }
            }
            catch
            {
                // Si el JSON no se puede leer, devolvemos sin datos normalizados
            }
        }

        return new TicketActivityItemDto(
            Action: a.Action,
            OccurredAt: a.OccurredAt,
            PerformedBy: a.PerformedBy,
            PerformedByName: a.PerformedByName,
            CorrelationId: a.CorrelationId,
            Title: title,
            AssigneeId: assigneeId,
            Reason: reason,
            OldStatus: oldStatus,
            NewStatus: newStatus,
            Comment: comment
        );
    }

    public static TicketActivityItemDto FromTicketActivity(TicketFlow.Domain.Entities.TicketActivity a)
    {
        return new TicketActivityItemDto(
            Action: a.Action,
            OccurredAt: a.OccurredAt,
            PerformedBy: a.PerformedBy,
            PerformedByName: a.PerformedByName,
            CorrelationId: a.CorrelationId,
            Title: a.Title,
            AssigneeId: a.AssigneeId,
            Reason: a.Reason,
            OldStatus: a.OldStatus,
            NewStatus: a.NewStatus,
            Comment: a.Comment
        );
    }
}
