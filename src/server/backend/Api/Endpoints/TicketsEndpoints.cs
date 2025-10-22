using Microsoft.AspNetCore.Authorization;

namespace TicketFlow.Api.Endpoints;

public static class TicketsEndpoints
{
    public static void MapTicketsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tickets")
            .WithTags("Tickets")
            .RequireAuthorization();

        // GET /api/tickets
        group.MapGet("/", [Authorize(Roles = "AGENT,ADMIN")] async () =>
        {
            // TODO: Implement GetAllTickets using repository
            var tickets = new[]
            {
                new { Id = 1, Title = "Ticket 1", Status = "OPEN", Priority = "HIGH" },
                new { Id = 2, Title = "Ticket 2", Status = "IN_PROGRESS", Priority = "MEDIUM" }
            };
            return Results.Ok(tickets);
        })
        .WithName("GetAllTickets")
        .WithOpenApi();

        // POST /api/tickets
        group.MapPost("/", [Authorize(Roles = "AGENT,ADMIN")] async (CreateTicketRequest request) =>
        {
            // TODO: Implement CreateTicket using use case
            var ticket = new
            {
                Id = new Random().Next(1000, 9999),
                request.Title,
                request.Description,
                Status = "OPEN",
                Priority = request.Priority ?? "MEDIUM",
                CreatedAt = DateTime.UtcNow
            };
            return Results.Created($"/api/tickets/{ticket.Id}", ticket);
        })
        .WithName("CreateTicket")
        .WithOpenApi();

        // PATCH /api/tickets/{id}/status
        group.MapPatch("/{id:int}/status", [Authorize(Roles = "AGENT,ADMIN")] async (int id, UpdateTicketStatusRequest request) =>
        {
            // TODO: Implement UpdateTicketStatus using use case
            var ticket = new
            {
                Id = id,
                Status = request.Status,
                UpdatedAt = DateTime.UtcNow
            };
            return Results.Ok(ticket);
        })
        .WithName("UpdateTicketStatus")
        .WithOpenApi();
    }
}

// DTOs
public record CreateTicketRequest(string Title, string Description, string? Priority);
public record UpdateTicketStatusRequest(string Status);
