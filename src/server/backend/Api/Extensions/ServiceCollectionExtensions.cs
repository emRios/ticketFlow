namespace TicketFlow.Api.Extensions;

/// <summary>
/// Extensiones para configurar servicios de la aplicación
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // TODO: Registrar use cases, políticas, etc.
        // services.AddScoped<CreateTicketUseCase>();
        // services.AddScoped<GetTicketByIdUseCase>();
        
        return services;
    }

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // TODO: Configurar DbContext, repositorios, etc.
        // services.AddDbContext<TicketFlowDbContext>(options =>
        //     options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        
        // services.AddScoped<ITicketRepository, TicketRepository>();
        // services.AddScoped<IUserRepository, UserRepository>();
        
        return services;
    }
}
