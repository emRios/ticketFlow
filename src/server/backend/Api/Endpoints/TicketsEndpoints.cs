using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TicketFlow.Api.DTOs;
using TicketFlow.Application.UseCases.Tickets.Commands.CreateTicket;
using TicketFlow.Application.UseCases.Tickets.Commands.ChangeTicketStatus;
using TicketFlow.Application.UseCases.Tickets.Queries.GetTickets;
using TicketFlow.Application.UseCases.Tickets.Queries.GetTicketById;

namespace TicketFlow.Api.Endpoints;

public static class TicketsEndpoints
{
    public static void MapTicketsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tickets")
            .WithTags("Tickets");

        // GET /api/tickets - Listar tickets
        group.MapGet("/", async (
            GetTicketsQueryHandler handler,
            string? status = null,
            string? priority = null,
            string? assignedTo = null) =>
        {
            var query = new GetTicketsQuery(status, priority, assignedTo);
            var tickets = await handler.HandleAsync(query);

            var response = tickets.Select(t => new TicketResponse(
                Id: t.Id,
                Title: t.Title,
                Description: t.Description,
                CreatedAt: t.CreatedAt
            ));

            return Results.Ok(response);
        })
        .WithName("GetTickets")
        .WithOpenApi()
        .Produces<IEnumerable<TicketResponse>>(200)
        .Produces(401);

        // POST /api/tickets - Crear ticket
        group.MapPost("/", async (
            CreateTicketHandler handler,
            CreateTicketRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return Results.BadRequest(new ErrorResponse("Title es requerido"));

            var userId = "test-user"; // Usuario de prueba

            var command = new CreateTicketCommand(
                Title: request.Title,
                Description: request.Description,
                Priority: request.Priority,
                CreatedBy: userId
            );

            var ticket = await handler.HandleAsync(command);

            var response = new TicketResponse(
                Id: ticket.Id,
                Title: ticket.Title,
                Description: ticket.Description,
                CreatedAt: ticket.CreatedAt
            );

            return Results.Created($"/api/tickets/{ticket.Id}", response);
        })
        .WithName("CreateTicket")
        .WithOpenApi()
        .Produces<TicketResponse>(201)
        .Produces<ErrorResponse>(400)
        .Produces(401);

        // PATCH /api/tickets/{id}/status - Cambiar estado
        group.MapPatch("/{id:guid}/status", [Authorize(Roles = "AGENT,ADMIN")] async (
            ClaimsPrincipal user,
            ChangeTicketStatusHandler handler,
            Guid id,
            ChangeStatusRequest request) =>
        {
            try
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                    ?? user.FindFirst("sub")?.Value 
                    ?? "system";

                var command = new ChangeTicketStatusCommand(
                    TicketId: id,
                    NewStatus: request.Status,
                    ChangedBy: userId
                );

                await handler.HandleAsync(command);

                return Results.NoContent();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("no encontrado"))
            {
                return Results.NotFound(new ErrorResponse("Ticket no encontrado"));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("transición inválida"))
            {
                return Results.BadRequest(new ErrorResponse("Transición de estado inválida", ex.Message));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse("Estado inválido", ex.Message));
            }
        })
        .WithName("UpdateTicketStatus")
        .WithOpenApi()
        .Produces(204)
        .Produces<ErrorResponse>(400)
        .Produces<ErrorResponse>(404)
        .Produces(401);

        // GET /api/tickets/{id} - Obtener ticket por ID
        group.MapGet("/{id:guid}", [Authorize] async (
            GetTicketByIdQueryHandler handler,
            Guid id) =>
        {
            var query = new GetTicketByIdQuery(id);
            var ticket = await handler.HandleAsync(query);

            if (ticket == null)
                return Results.NotFound(new ErrorResponse("Ticket no encontrado"));

            var response = new TicketResponse(
                Id: ticket.Id,
                Title: ticket.Title,
                Description: ticket.Description,
                CreatedAt: ticket.CreatedAt
            );

            return Results.Ok(response);
        })
        .WithName("GetTicketById")
        .WithOpenApi()
        .Produces<TicketResponse>(200)
        .Produces<ErrorResponse>(404)
        .Produces(401);
    }
}

