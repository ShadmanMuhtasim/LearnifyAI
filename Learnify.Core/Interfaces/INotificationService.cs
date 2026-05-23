namespace Learnify.Core.Interfaces;

/// <summary>
/// Represents a notification message to be processed by background workers.
/// </summary>
public class NotificationMessage
{
    /// <summary>
    /// The type of notification (e.g., "WelcomeEmail", "CourseEnrollment", "AIProcessing").
    /// </summary>
    public string NotificationType { get; set; } = string.Empty;

    /// <summary>
    /// Recipient email address.
    /// </summary>
    public string Recipient { get; set; } = string.Empty;

    /// <summary>
    /// Recipient display name.
    /// </summary>
    public string? RecipientName { get; set; }

    /// <summary>
    /// Email subject line.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Email body content (supports HTML).
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Optional metadata for advanced processing (e.g., AI queue parameters).
    /// </summary>
    public Dictionary<string, string?>? Metadata { get; set; }

    /// <summary>
    /// When the notification was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Service contract for sending notifications asynchronously.
/// Implementations can include email, SMS, push notifications, or webhook dispatching.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Queues a notification for asynchronous processing by the background worker.
    /// </summary>
    /// <param name="message">The notification message to queue.</param>
    /// <returns>True if the message was successfully queued.</returns>
    Task QueueNotificationAsync(NotificationMessage message);

    /// <summary>
    /// Sends a notification synchronously (for testing or critical-path scenarios).
    /// </summary>
    /// <param name="message">The notification message to send.</param>
    Task SendNotificationAsync(NotificationMessage message);
}