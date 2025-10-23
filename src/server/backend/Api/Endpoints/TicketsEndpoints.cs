// TODO: Re-habilitar cuando JWT esté configurado
// using Microsoft.AspNetCore.Authorization;
// using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TicketFlow.Api.DTOs;
using TicketFlow.Application.UseCases.Tickets.Commands.CreateTicket;
using TicketFlow.Application.UseCases.Tickets.Commands.ChangeTicketStatus;
using TicketFlow.Application.UseCases.Tickets.Commands;
using TicketFlow.Application.UseCases.Tickets.Queries.GetTickets;
using TicketFlow.Application.UseCases.Tickets.Queries.GetTicketById;
using TicketFlow.Application.Interfaces;

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
            Infrastructure.Persistence.TicketFlowDbContext db,
            string? status = null,
            string? priority = null,
            string? assignedTo = null) =>
        {
            var query = new GetTicketsQuery(status, priority, assignedTo);
            var tickets = await handler.HandleAsync(query);

            // Obtener IDs de usuarios asignados para resolver nombres
            var assignedUserIds = tickets
                .Where(t => !string.IsNullOrWhiteSpace(t.AssignedTo))
                .Select(t => t.AssignedTo!)
                .Distinct()
                .Select(id => Guid.TryParse(id, out var guid) ? guid : (Guid?)null)
                .Where(g => g.HasValue)
                .Select(g => g!.Value)
                .ToList();

            var userNames = await db.Users
                .Where(u => assignedUserIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Name })
                .ToDictionaryAsync(x => x.Id.ToString(), x => x.Name);

            var response = tickets.Select(t => new TicketResponse(
                Id: t.Id,
                Title: t.Title,
                Description: t.Description,
                Status: t.Status,
                Priority: t.Priority,
                AssignedTo: t.AssignedTo,
                AssignedToName: t.AssignedTo != null && userNames.TryGetValue(t.AssignedTo, out var name) ? name : null,
                CreatedAt: t.CreatedAt,
                UpdatedAt: t.UpdatedAt
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
            ITicketRepository ticketRepo,
            Infrastructure.Persistence.TicketFlowDbContext db,
            HttpContext http,
            CreateTicketRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return Results.BadRequest(new ErrorResponse("Title es requerido"));

            // Extraer X-UserId para trazabilidad
            var userId = http.Request.Headers.TryGetValue("X-UserId", out var uid) ? uid.ToString() : "anonymous";

            var command = new CreateTicketCommand(
                Title: request.Title,
                Description: request.Description,
                Priority: request.Priority,
                CreatedBy: userId
            );

            var result = await handler.HandleAsync(command);

            // Obtener ticket completo para respuesta
            var ticket = await ticketRepo.GetByIdAsync(result.TicketId);

            if (ticket == null)
            {
                return Results.Problem("Ticket creado pero no se pudo recuperar");
            }

            // Resolver nombre del asignado si existe
            string? assignedToName = null;
            if (result.AssignedTo.HasValue)
            {
                var user = await db.Users.FindAsync(result.AssignedTo.Value);
                assignedToName = user?.Name;
            }

            var response = new TicketCreatedResponse(
                Id: result.TicketId,
                Title: ticket.Title,
                Description: ticket.Description,
                Status: result.Status,
                Priority: ticket.Priority,
                AssignedTo: result.AssignedTo?.ToString(),
                AssignedToName: assignedToName,
                AutoAssigned: result.AutoAssigned,
                AssignmentReason: result.AssignmentReason,
                CreatedAt: ticket.CreatedAt,
                UpdatedAt: ticket.UpdatedAt
            );

            return Results.Created($"/api/tickets/{result.TicketId}", response);
        })
        .WithName("CreateTicket")
        .WithOpenApi()
        .Produces<TicketCreatedResponse>(201)
        .Produces<ErrorResponse>(400)
        .Produces(401);

        // POST /api/tickets/{id}/assign - Asignar ticket a un usuario
        // Solo permite reasignación si el ticket está en estado nuevo o en-proceso
        group.MapPost("/{id:guid}/assign", async (
            AssignTicketHandler handler,
            HttpContext http,
            Guid id,
            AssignTicketRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.AssigneeId))
                return Results.BadRequest(new ErrorResponse("AssigneeId es requerido"));

            if (!Guid.TryParse(request.AssigneeId, out var assigneeGuid))
                return Results.BadRequest(new ErrorResponse("AssigneeId debe ser un GUID válido"));

            var userId = http.Request.Headers.TryGetValue("X-UserId", out var uid) ? uid.ToString() : "anonymous";
            if (!Guid.TryParse(userId, out var performedByGuid))
                performedByGuid = Guid.Empty;

            var command = new AssignTicketCommand
            {
                TicketId = id,
                AssigneeId = assigneeGuid,
                PerformedBy = performedByGuid,
                Reason = request.Reason
            };

            var result = await handler.HandleAsync(command);

            if (!result.IsSuccess)
                return Results.BadRequest(new ErrorResponse(result.Error ?? "Error al asignar ticket"));

            return Results.NoContent();
        })
        .WithName("AssignTicket")
        .WithOpenApi()
        .Produces(204)
        .Produces<ErrorResponse>(400)
        .Produces<ErrorResponse>(404)
        .Produces(401);

        // PATCH /api/tickets/{id}/status - Cambiar estado
        // TODO: Re-habilitar [Authorize(Roles = "AGENT,ADMIN")] cuando JWT esté configurado
        group.MapPatch("/{id:guid}/status", async (
            ChangeTicketStatusHandler handler,
            HttpContext http,
            Guid id,
            ChangeStatusRequest request) =>
        {
            try
            {
                // Extraer X-UserId para trazabilidad (reemplazar por JWT en prod)
                var userId = http.Request.Headers.TryGetValue("X-UserId", out var uid) ? uid.ToString() : "anonymous";

                var newStatus = request.GetStatus();
                if (string.IsNullOrWhiteSpace(newStatus))
                {
                    return Results.BadRequest(new ErrorResponse("El campo 'status' o 'newStatus' es requerido"));
                }

                var command = new ChangeTicketStatusCommand(
                    TicketId: id,
                    NewStatus: newStatus,
                    ChangedBy: userId,
                    Comment: request.Comment
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
        // TODO: Re-habilitar [Authorize] cuando JWT esté configurado
        group.MapGet("/{id:guid}", async (
            GetTicketByIdQueryHandler handler,
            Infrastructure.Persistence.TicketFlowDbContext db,
            Guid id) =>
        {
            var query = new GetTicketByIdQuery(id);
            var ticket = await handler.HandleAsync(query);

            if (ticket == null)
                return Results.NotFound(new ErrorResponse("Ticket no encontrado"));

            // Resolver nombre del asignado si existe
            string? assignedToName = null;
            if (!string.IsNullOrWhiteSpace(ticket.AssignedTo) && Guid.TryParse(ticket.AssignedTo, out var assignedGuid))
            {
                var user = await db.Users.FindAsync(assignedGuid);
                assignedToName = user?.Name;
            }

            var response = new TicketResponse(
                Id: ticket.Id,
                Title: ticket.Title,
                Description: ticket.Description,
                Status: ticket.Status,
                Priority: ticket.Priority,
                AssignedTo: ticket.AssignedTo,
                AssignedToName: assignedToName,
                CreatedAt: ticket.CreatedAt,
                UpdatedAt: ticket.UpdatedAt
            );

            return Results.Ok(response);
        })
        .WithName("GetTicketById")
        .WithOpenApi()
        .Produces<TicketResponse>(200)
        .Produces<ErrorResponse>(404)
        .Produces(401);

        // GET /api/tickets/{id}/activity - Historial usando TicketActivities
        group.MapGet("/{id:guid}/activity", async (
            Infrastructure.Persistence.TicketFlowDbContext db,
            Guid id) =>
        {
            var activities = await db.TicketActivities
                .Where(a => a.TicketId == id)
                .OrderBy(a => a.OccurredAt)
                .ToListAsync();

            var items = activities.Select(a => TicketActivityItemDto.FromTicketActivity(a));

            return Results.Ok(items);
        })
        .WithName("GetTicketActivity")
        .WithOpenApi()
        .Produces<IEnumerable<TicketActivityItemDto>>(200)
        .Produces(401);
    }
}

