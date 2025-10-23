using System.Text.Json;

namespace TicketFlow.Api.DTOs;

public record TicketActivityItem(
    string Action,
    DateTime OccurredAt,
    string? PerformedBy,
    JsonElement? Data
);
