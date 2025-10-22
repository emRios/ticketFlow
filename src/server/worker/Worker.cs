using TicketFlow.Worker.Processors;

namespace TicketFlow.Worker;

/// <summary>
/// Worker principal para procesamiento de Outbox
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly OutboxProcessor _outboxProcessor;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);

    public Worker(
        ILogger<Worker> logger,
        OutboxProcessor outboxProcessor)
    {
        _logger = logger;
        _outboxProcessor = outboxProcessor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Worker iniciado. Polling cada {Interval} segundos", _pollingInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _outboxProcessor.ProcessAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico en el procesamiento del Outbox");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("Outbox Worker detenido.");
    }
}
