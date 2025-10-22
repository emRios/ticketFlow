namespace TicketFlow.Api.DTOs;

// ========== Responses ==========

public record TicketResponse(
    Guid Id,
    string Title,
    string Description,
    DateTime CreatedAt
);

public record UserResponse(
    string UserId,
    string Username,
    string Role,
    string Email
);

public record ErrorResponse(
    string Error,
    string? Detail = null
);
