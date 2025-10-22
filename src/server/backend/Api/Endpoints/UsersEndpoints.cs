namespace TicketFlow.Api.Endpoints;

public static class UsersEndpoints
{
    public static void MapUsersEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users");

        // GET /api/users
        group.MapGet("/", async () =>
        {
            // TODO: Implement GetAllUsers
            return Results.Ok(new[] { new { Id = 1, Name = "Sample User" } });
        })
        .WithName("GetAllUsers");
    }
}
