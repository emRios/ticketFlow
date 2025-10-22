using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TicketFlow.Api.DTOs;

namespace TicketFlow.Api.Endpoints;

public static class UsersEndpoints
{
    public static void MapUsersEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api")
            .WithTags("Users");

        // GET /api/me - Información del usuario autenticado
        group.MapGet("/me", [Authorize] (ClaimsPrincipal user) =>
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? user.FindFirst("sub")?.Value 
                ?? "unknown";
            
            var username = user.FindFirst(ClaimTypes.Name)?.Value 
                ?? user.FindFirst("name")?.Value 
                ?? "Unknown User";
            
            var role = user.FindFirst(ClaimTypes.Role)?.Value 
                ?? user.FindFirst("role")?.Value 
                ?? "USER";
            
            var email = user.FindFirst(ClaimTypes.Email)?.Value 
                ?? user.FindFirst("email")?.Value 
                ?? "no-email@example.com";

            var response = new UserResponse(
                UserId: userId,
                Username: username,
                Role: role,
                Email: email
            );

            return Results.Ok(response);
        })
        .WithName("GetCurrentUser")
        .WithOpenApi()
        .Produces<UserResponse>(200)
        .Produces(401);
    }
}
