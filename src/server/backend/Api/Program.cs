using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using TicketFlow.Api.Auth;
using TicketFlow.Api.Endpoints;
using TicketFlow.Api.Extensions;
using TicketFlow.Application.Interfaces;
using TicketFlow.Application.Interfaces.Repositories;
using TicketFlow.Application.UseCases.Tickets.Commands.CreateTicket;
using TicketFlow.Application.UseCases.Tickets.Commands.ChangeTicketStatus;
using TicketFlow.Application.UseCases.Tickets.Commands;
using TicketFlow.Application.UseCases.Tickets.Queries.GetTickets;
using TicketFlow.Application.UseCases.Tickets.Queries.GetTicketById;
using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Services;
using TicketFlow.Infrastructure.Persistence;
using TicketFlow.Infrastructure.Repositories;
using TicketFlow.Infrastructure.Services.Assignment;

var builder = WebApplication.CreateBuilder(args);

// Configurar CORS para desarrollo
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174", "http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add services to the container
builder.Services.AddEndpointsApiExplorer();

// Configurar Swagger con soporte JWT
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TicketFlow API",
        Version = "v1",
        Description = "API para gestión de tickets con autenticación JWT"
    });

    // Configurar JWT en Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese 'Bearer' [espacio] y luego su token JWT"
    });

    // TODO: Re-habilitar cuando JWT esté configurado
    // Este requisito global fuerza autenticación en todos los endpoints
    /*
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    */
});

// Configurar autenticación JWT
// TODO: Re-habilitar cuando JWT esté configurado
// builder.Services.AddAuthServices(builder.Configuration);

// Autorization mínima sin autenticación (desarrollo)
builder.Services.AddAuthorization();

// Configurar DbContext con PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<TicketFlowDbContext>(options =>
    options.UseNpgsql(connectionString, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

// Registrar repositorios e infraestructura
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Registrar estrategia de asignación automática
builder.Services.AddScoped<IAssignmentStrategy, ByLoadAssignmentStrategy>();

// Registrar handlers de Application Layer
builder.Services.AddScoped<CreateTicketHandler>();
builder.Services.AddScoped<ChangeTicketStatusHandler>();
builder.Services.AddScoped<AssignTicketHandler>();
builder.Services.AddScoped<GetTicketsQueryHandler>();
builder.Services.AddScoped<GetTicketByIdQueryHandler>();

var app = builder.Build();

// Crear esquema en Development (drop + create) para reflejar cambios de modelo sin migraciones
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TicketFlowDbContext>();
    // Apply pending migrations automatically on startup
    await db.Database.MigrateAsync();
    
    // Seed: Crear usuario admin si no existe
    var adminEmail = "admin@ticketflow.com";
    var adminExists = await db.Users.AnyAsync(u => u.Email == adminEmail);
    if (!adminExists)
    {
        var adminUser = User.Create(adminEmail, "Administrador", "ADMIN");
        await db.Users.AddAsync(adminUser);
        await db.SaveChangesAsync();
        app.Logger.LogInformation("✅ Usuario admin creado: {Email}", adminEmail);
    }
}

// Configure the HTTP request pipeline
// TODO: Swagger deshabilitado temporalmente para testing sin auth
/*
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TicketFlow API v1");
    });
}
*/

app.UseHttpsRedirection();

// Habilitar CORS
app.UseCors("AllowFrontend");

// Middleware de autorización (sin autenticación)
app.UseAuthorization();

// Map endpoints
app.MapTicketsEndpoints();
app.MapUsersEndpoints();
app.MapAuthEndpoints();

// Health check endpoint (para Docker healthcheck)
app.MapGet("/health", () => Results.Ok(new 
{ 
    status = "Healthy",
    service = "TicketFlow API",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName
}))
.WithName("HealthCheck")
.WithTags("Health")
.Produces(200)
.WithOpenApi(operation => new(operation)
{
    Summary = "Health Check",
    Description = "Verifica que la API esté funcionando correctamente"
});

app.Run();

