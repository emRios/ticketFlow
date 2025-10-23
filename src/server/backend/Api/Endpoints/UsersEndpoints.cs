using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TicketFlow.Api.DTOs;
using TicketFlow.Domain.Entities;
using TicketFlow.Infrastructure.Persistence;

namespace TicketFlow.Api.Endpoints;

public static class UsersEndpoints
{
    public static void MapUsersEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api")
            .WithTags("Users");

        // GET /api/users - Listar usuarios (solo dev/testing)
        group.MapGet("/users", async (TicketFlowDbContext db, string? role) =>
        {
            var query = db.Users.AsQueryable();

            // Filtrar por rol si se proporciona
            if (!string.IsNullOrWhiteSpace(role))
            {
                var roleUpper = role.ToUpperInvariant();
                query = query.Where(u => u.Role == roleUpper);
            }

            var users = await query
                .OrderBy(u => u.Name)
                .Select(u => new UserResponse(
                    u.Id.ToString(),
                    u.Name,
                    u.Role,
                    u.Email
                ))
                .ToListAsync();

            return Results.Ok(users);
        })
        .WithName("ListUsers")
        .WithOpenApi()
        .Produces<IEnumerable<UserResponse>>(200);

        // POST /api/users - Registrar usuario
        group.MapPost("/users", async (TicketFlowDbContext db, RegisterUserRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new ErrorResponse("Email y Name son requeridos"));
            }

            var role = string.IsNullOrWhiteSpace(request.Role) ? "AGENT" : request.Role.ToUpperInvariant();
            var user = User.Create(request.Email.Trim(), request.Name.Trim(), role);
            await db.Users.AddAsync(user);
            await db.SaveChangesAsync();

            var response = new UserResponse(
                UserId: user.Id.ToString(),
                Username: user.Name,
                Role: user.Role,
                Email: user.Email
            );

            return Results.Created($"/api/users/{user.Id}", response);
        })
        .WithName("RegisterUser")
        .WithOpenApi()
        .Produces<UserResponse>(201)
        .Produces<ErrorResponse>(400);

        // PUT /api/users/{id} - Actualizar usuario
        group.MapPut("/users/{id:guid}", async (Guid id, TicketFlowDbContext db, UpdateUserRequest request) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
            
            if (user == null)
            {
                return Results.NotFound(new ErrorResponse($"Usuario {id} no encontrado"));
            }

            try
            {
                user.Update(
                    name: request.Name?.Trim(),
                    email: request.Email?.Trim(),
                    role: request.Role
                );
                
                await db.SaveChangesAsync();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse(ex.Message));
            }

            var response = new UserResponse(
                UserId: user.Id.ToString(),
                Username: user.Name,
                Role: user.Role,
                Email: user.Email
            );

            return Results.Ok(response);
        })
        .WithName("UpdateUser")
        .WithOpenApi()
        .Produces<UserResponse>(200)
        .Produces<ErrorResponse>(404)
        .Produces<ErrorResponse>(400);

        // DELETE /api/users/{id} - Eliminar usuario
        group.MapDelete("/users/{id:guid}", async (Guid id, TicketFlowDbContext db) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
            
            if (user == null)
            {
                return Results.NotFound(new ErrorResponse($"Usuario {id} no encontrado"));
            }

            // Prevenir eliminación del admin principal
            if (user.Email.Equals("admin@ticketflow.com", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new ErrorResponse("No se puede eliminar el usuario administrador principal"));
            }

            db.Users.Remove(user);
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .WithName("DeleteUser")
        .WithOpenApi()
        .Produces(204)
        .Produces<ErrorResponse>(404)
        .Produces<ErrorResponse>(400);

        // GET /api/me - Información del usuario (desde BD, header X-UserId opcional)
        group.MapGet("/me", async (TicketFlowDbContext db, HttpContext http) =>
        {
            User? user = null;

            if (http.Request.Headers.TryGetValue("X-UserId", out var userIdHeader))
            {
                if (Guid.TryParse(userIdHeader.ToString(), out var userId))
                {
                    user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                }
            }

            // Si no se envió X-UserId o no se encontró, tomar el primero
            user ??= await db.Users.OrderBy(u => u.Name).FirstOrDefaultAsync();

            // Si no hay usuarios, crear uno por defecto para desarrollo
            if (user == null)
            {
                user = User.Create("agent@ticketflow.com", "agent.demo", "AGENT");
                await db.Users.AddAsync(user);
                await db.SaveChangesAsync();
            }

            var response = new UserResponse(
                UserId: user.Id.ToString(),
                Username: user.Name,
                Role: user.Role,
                Email: user.Email
            );

            return Results.Ok(response);
        })
        .WithName("GetCurrentUser")
        .WithOpenApi()
        .Produces<UserResponse>(200)
        .Produces(401);
    }
}
