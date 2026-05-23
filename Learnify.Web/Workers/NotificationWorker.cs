using Learnify.Core.Interfaces;
using Learnify.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Learnify.Web.Workers;

/// <summary>
/// Background worker that processes the notification channel.
/// Registered as a HostedService in Program.cs.
/// </summary>
public class NotificationWorker : BackgroundService
{
    private readonly ILogger<NotificationWorker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public NotificationWorker(
        ILogger<NotificationWorker> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[WORKER] NotificationWorker started at {Timestamp}.", DateTime.UtcNow);

        try
        {
            // Get the EmailNotificationService to start processing the channel
            using var scope = _serviceProvider.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            if (notificationService is EmailNotificationService emailService)
            {
                await emailService.ProcessNotificationsAsync(stoppingToken);
            }
            else
            {
                _logger.LogWarning("[WORKER] INotificationService is not EmailNotificationService. " +
                    "Channel-based processing may not work correctly.");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[WORKER] NotificationWorker stopping gracefully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WORKER] NotificationWorker terminated unexpectedly.");
        }
        finally
        {
            _logger.LogInformation("[WORKER] NotificationWorker stopped at {Timestamp}.", DateTime.UtcNow);
        }
    }
}