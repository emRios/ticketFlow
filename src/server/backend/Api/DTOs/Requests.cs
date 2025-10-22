namespace TicketFlow.Api.DTOs;

// ========== Requests ==========

public record CreateTicketRequest(
    string Title,
    string Description,
    string? Priority = "MEDIUM"
);

public record ChangeStatusRequest(
    string Status
);

// ========== Query Parameters ==========

public record TicketFilters(
    string? Status = null,
    string? Priority = null,
    string? AssignedTo = null
);
