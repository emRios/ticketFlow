namespace TicketFlow.Api.DTOs;

// ========== Responses ==========

public record TicketResponse(
    Guid Id,
    string Title,
    string Description,
    string Status,
    string Priority,
    string? AssignedTo,
    string? AssignedToName,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record TicketCreatedResponse(
    Guid Id,
    string Title,
    string Description,
    string Status,
    string Priority,
    string? AssignedTo,
    string? AssignedToName,
    bool AutoAssigned,
    string? AssignmentReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record UserResponse(
    string UserId,
    string Username,
    string Role,
    string Email
);

public record LoginResponse(
    string UserId,
    string Username,
    string Email,
    string Role
);

public record ErrorResponse(
    string Error,
    string? Detail = null
);
