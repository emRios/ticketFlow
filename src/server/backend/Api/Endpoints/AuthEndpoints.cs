using Microsoft.EntityFrameworkCore;
using TicketFlow.Api.DTOs;
using TicketFlow.Infrastructure.Persistence;

namespace TicketFlow.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth");

        // POST /api/auth/login - Login simple (dev mode sin password real)
        group.MapPost("/login", async (TicketFlowDbContext db, LoginRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return Results.BadRequest(new ErrorResponse("Email es requerido"));
            }

            // Buscar usuario por email (modo dev: sin validación de password)
            var user = await db.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email.Trim());

            if (user == null)
            {
                // En modo dev, devolvemos 404 para que el frontend muestre
                // un mensaje claro de "usuario no encontrado" y permita registrar.
                return Results.NotFound(new ErrorResponse("Usuario no encontrado"));
            }

            var response = new LoginResponse(
                UserId: user.Id.ToString(),
                Username: user.Name,
                Email: user.Email,
                Role: user.Role
            );

            return Results.Ok(response);
        })
        .WithName("Login")
        .WithOpenApi()
        .Produces<LoginResponse>(200)
        .Produces<ErrorResponse>(400)
        .Produces(401);
    }
}
