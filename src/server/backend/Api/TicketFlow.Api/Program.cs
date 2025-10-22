using TicketFlow.Api.Auth;
using TicketFlow.Api.Endpoints;
using TicketFlow.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configurar servicios de la aplicación
// builder.Services.AddApplicationServices();
// builder.Services.AddInfrastructureServices(builder.Configuration);
// builder.Services.AddAuthServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map endpoints
app.MapTicketsEndpoints();
app.MapUsersEndpoints();

app.Run();

