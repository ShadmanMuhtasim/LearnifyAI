using Learnify.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace Learnify.Infrastructure.Services;

/// <summary>
/// Mock email notification service that logs notification details instead of sending real emails.
/// Designed for easy replacement with a real SMTP implementation (e.g., SendGrid, Mailgun) later.
/// </summary>
public class EmailNotificationService : INotificationService
{
    private readonly Channel<NotificationMessage> _notificationChannel;
    private readonly ILogger<EmailNotificationService> _logger;

    /// <summary>
    /// Shared channel for background notification processing.
    /// Created once and shared via static access to ensure all producers use the same queue.
    /// </summary>
    private static readonly Channel<NotificationMessage> _sharedChannel = CreateBoundedChannel(1000);

    private static Channel<NotificationMessage> CreateBoundedChannel(int maxSize)
    {
        return Channel.CreateBounded<NotificationMessage>(new BoundedChannelOptions(maxSize)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public EmailNotificationService(ILogger<EmailNotificationService> logger)
    {
        _logger = logger;
        _notificationChannel = _sharedChannel;
    }

    /// <summary>
    /// Gets the shared notification channel for use by background workers.
    /// </summary>
    public static Channel<NotificationMessage> SharedChannel => _sharedChannel;

    /// <summary>
    /// Queues a notification for asynchronous processing by the background worker.
    /// This is the preferred method for production use — it returns immediately.
    /// </summary>
    public async Task QueueNotificationAsync(NotificationMessage message)
    {
        _logger.LogInformation("[QUEUE] Notification queued for '{Recipient}' with type '{NotificationType}'.",
            message.Recipient, message.NotificationType);

        await _notificationChannel.Writer.WriteAsync(message);

        _logger.LogInformation("[QUEUED] Notification written to channel for '{Recipient}'.", message.Recipient);
    }

    /// <summary>
    /// Sends a notification synchronously by writing to the channel and awaiting completion.
    /// Useful for testing or critical-path scenarios where immediate processing is required.
    /// </summary>
    public async Task SendNotificationAsync(NotificationMessage message)
    {
        _logger.LogInformation("[SEND] Processing notification synchronously for '{Recipient}'.", message.Recipient);

        // Simulate email sending delay
        await Task.Delay(TimeSpan.FromMilliseconds(50));

        _logger.LogInformation("[SENT] Email sent to '{Recipient}' — Subject: '{Subject}', Type: '{NotificationType}'.",
            message.Recipient, message.Subject, message.NotificationType);
    }

    /// <summary>
    /// Starts the notification processing loop. Called by the BackgroundService.
    /// </summary>
    public async Task ProcessNotificationsAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[WORKER] EmailNotificationService background worker started.");

        try
        {
            await foreach (var message in _notificationChannel.Reader.ReadAllAsync(stoppingToken))
            {
                await ProcessSingleNotificationAsync(message, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[WORKER] EmailNotificationService background worker is stopping.");
        }
    }

    private async Task ProcessSingleNotificationAsync(NotificationMessage message, CancellationToken stoppingToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Simulate email sending work (replace with real SMTP/send in production)
            await Task.Delay(TimeSpan.FromMilliseconds(30), stoppingToken);

            _logger.LogInformation(
                "[SENT] ✅ Email delivered to '{Recipient}' (Name: {RecipientName}) — Type: '{NotificationType}', Subject: '{Subject}'. " +
                "Metadata: {MetadataCount} keys.",
                message.Recipient,
                message.RecipientName ?? "N/A",
                message.NotificationType,
                message.Subject,
                message.Metadata?.Count ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[ERROR] Failed to send notification to '{Recipient}' — Type: '{NotificationType}'.",
                message.Recipient, message.NotificationType);
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogDebug(
                "[PROFILE] Notification processing completed in {ElapsedMs}ms for '{Recipient}'.",
                stopwatch.ElapsedMilliseconds, message.Recipient);
        }
    }
}