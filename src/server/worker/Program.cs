using Microsoft.EntityFrameworkCore;
using TicketFlow.Infrastructure.Outbox;
using TicketFlow.Infrastructure.Persistence;
using TicketFlow.Worker;
using TicketFlow.Worker.Messaging;
using TicketFlow.Worker.Messaging.RabbitMq;
using TicketFlow.Worker.Processors;

var builder = Host.CreateApplicationBuilder(args);

// Configurar DbContext con PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<TicketFlowDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configurar RabbitMQ - Leer desde appsettings
var rabbitMqHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
var rabbitMqPort = builder.Configuration.GetValue<int>("RabbitMQ:Port", 5672);
var rabbitMqUsername = builder.Configuration["RabbitMQ:Username"] ?? "guest";
var rabbitMqPassword = builder.Configuration["RabbitMQ:Password"] ?? "guest";
var rabbitMqExchange = builder.Configuration["RabbitMQ:Exchange"] ?? "tickets";

// Registrar RabbitMQ Publisher como Singleton
builder.Services.AddSingleton<IMessagePublisher>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<RabbitMqPublisher>>();
    return new RabbitMqPublisher(
        logger, 
        rabbitMqHost, 
        rabbitMqExchange,
        rabbitMqUsername,
        rabbitMqPassword,
        rabbitMqPort);
});

// Registrar servicios del Outbox
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddSingleton<OutboxProcessor>();

// Registrar Worker principal
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

// ============================================================
// INICIALIZAR TOPOLOGÍA DE RABBITMQ AL ARRANCAR
// ============================================================
var logger = host.Services.GetRequiredService<ILogger<Program>>();

try
{
    logger.LogInformation("🔧 Inicializando infraestructura...");

    // 1. Aplicar migraciones de base de datos (solo en Development)
    if (builder.Environment.IsDevelopment())
    {
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TicketFlowDbContext>();
        
        await context.Database.MigrateAsync();
        logger.LogInformation("✅ Migraciones de base de datos aplicadas");
    }

    // 2. Inicializar topología de RabbitMQ (SIEMPRE)
    var topologyBootstrapper = new RabbitTopologyBootstrapper(
        host.Services.GetRequiredService<ILogger<RabbitTopologyBootstrapper>>(),
        rabbitMqHost,
        rabbitMqPort,
        rabbitMqUsername,
        rabbitMqPassword
    );

    topologyBootstrapper.InitializeTopology();
    
    logger.LogInformation("✅ Infraestructura inicializada correctamente");
}
catch (Exception ex)
{
    logger.LogError(ex, "❌ Error crítico al inicializar infraestructura");
    throw; // Fallar rápido si no se puede inicializar
}

// Ejecutar el Worker
host.Run();
