namespace TicketFlow.Api.DTOs;

// ========== Requests ==========

public record CreateTicketRequest(
    string Title,
    string Description,
    string? Priority = "MEDIUM"
);

public record ChangeStatusRequest(
    string? Status = null,
    string? NewStatus = null,
    string? Comment = null
)
{
    // Permite usar tanto "status" como "newStatus"
    public string GetStatus() => NewStatus ?? Status ?? string.Empty;
};

public record AssignTicketRequest(
    string AssigneeId,
    string? Reason = null
);

public record RegisterUserRequest(
    string Email,
    string Name,
    string? Role = "AGENT"
);

public record UpdateUserRequest(
    string? Name = null,
    string? Email = null,
    string? Role = null
);

public record LoginRequest(
    string Email,
    string? Password = null  // Opcional para modo dev
);

// ========== Query Parameters ==========

public record TicketFilters(
    string? Status = null,
    string? Priority = null,
    string? AssignedTo = null
);
