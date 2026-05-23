using Learnify.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Learnify.Web.Workers;

/// <summary>
/// Background worker that processes AI jobs from the notification queue.
/// Currently acts as a placeholder for future AI processing queue integration.
/// </summary>
public class AiProcessingWorker : BackgroundService
{
    private readonly ILogger<AiProcessingWorker> _logger;
    private int _executionCount;

    public AiProcessingWorker(ILogger<AiProcessingWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AI Processing Worker is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("AI Processing worker running. Period: {Period}", DateTimeOffset.UtcNow);
                
                // Placeholder: In a production system, this would poll an AI job queue
                // and process AI requests asynchronously.
                // For now, we just log that the worker is alive.
                await Task.Delay(60_000, stoppingToken); // Check every minute
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AI Processing Worker");
                await Task.Delay(5_000, stoppingToken); // Wait on error
            }
        }

        _logger.LogInformation("AI Processing Worker is stopping.");
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("AI Processing Worker stopped gracefully.");
        return base.StopAsync(cancellationToken);
    }
}