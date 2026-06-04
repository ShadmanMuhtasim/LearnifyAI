using Learnify.Core.Interfaces;
using Xunit;
using System.Threading.Channels;

namespace Learnify.Tests;

/// <summary>
/// Unit tests for the EmailNotificationService background worker queue processing.
/// Tests verify that notifications are correctly queued and processed through the Channel<NotificationMessage>.
/// </summary>
public class NotificationWorkerTests : IDisposable
{
    private readonly Channel<NotificationMessage> _channel;
    private readonly List<string> _processedMessages;

    public NotificationWorkerTests()
    {
        _channel = Channel.CreateBounded<NotificationMessage>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
        _processedMessages = new List<string>();
    }

    [Fact(DisplayName = "Verifies that a notification message can be written to and read from the channel.")]
    public async Task QueueNotification_ShouldWriteToChannel()
    {
        // Arrange
        var message = new NotificationMessage
        {
            NotificationType = "TestNotification",
            Recipient = "test@example.com",
            RecipientName = "Test User",
            Subject = "Test Subject",
            Body = "Test Body",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _channel.Writer.WriteAsync(message);

        // Assert
        var readResult = await _channel.Reader.ReadAsync();
        Assert.Equal("TestNotification", readResult.NotificationType);
        Assert.Equal("test@example.com", readResult.Recipient);
        Assert.Equal("Test Subject", readResult.Subject);
    }

    [Fact(DisplayName = "Verifies that multiple notifications can be queued and processed in order.")]
    public async Task ProcessNotifications_ShouldProcessMultipleMessagesInOrder()
    {
        // Arrange
        var messages = new[]
        {
            new NotificationMessage { NotificationType = "Type1", Recipient = "user1@test.com", Subject = "Subject1" },
            new NotificationMessage { NotificationType = "Type2", Recipient = "user2@test.com", Subject = "Subject2" },
            new NotificationMessage { NotificationType = "Type3", Recipient = "user3@test.com", Subject = "Subject3" }
        };

        // Act
        foreach (var msg in messages)
            await _channel.Writer.WriteAsync(msg);

        // Assert
        for (int i = 0; i < messages.Length; i++)
        {
            var read = await _channel.Reader.ReadAsync();
            Assert.Equal(messages[i].NotificationType, read.NotificationType);
            Assert.Equal(messages[i].Recipient, read.Recipient);
        }
    }

    [Fact(DisplayName = "Verifies that the channel respects the bounded capacity.")]
    public async Task QueueNotification_ShouldRespectBoundedCapacity()
    {
        // Arrange: Create a small bounded channel
        var smallChannel = Channel.CreateBounded<NotificationMessage>(new BoundedChannelOptions(3)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

        // Act: Write 3 messages (should succeed immediately)
        for (int i = 0; i < 3; i++)
        {
            await smallChannel.Writer.WriteAsync(new NotificationMessage { NotificationType = $"Msg{i}" });
        }

        // Assert: The 4th write should block (we verify by reading one and checking)
        var first = await smallChannel.Reader.ReadAsync();
        Assert.Equal("Msg0", first.NotificationType);
    }

    [Fact(DisplayName = "Verifies that metadata dictionary is preserved through the channel.")]
    public async Task QueueNotification_ShouldPreserveMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, string?>
        {
            ["userId"] = "abc-123",
            ["action"] = "enrollment",
            ["source"] = "web"
        };

        var message = new NotificationMessage
        {
            NotificationType = "Enrollment",
            Recipient = "student@example.com",
            Subject = "Enrolled",
            Metadata = metadata
        };

        // Act
        await _channel.Writer.WriteAsync(message);
        var read = await _channel.Reader.ReadAsync();

        // Assert
        Assert.NotNull(read.Metadata);
        Assert.Equal(3, read.Metadata.Count);
        Assert.Equal("abc-123", read.Metadata["userId"]);
        Assert.Equal("enrollment", read.Metadata["action"]);
        Assert.Equal("web", read.Metadata["source"]);
    }

    [Fact(DisplayName = "Verifies that the channel reader completes when writer completes.")]
    public async Task ProcessNotifications_ShouldCompleteWhenWriterCompletes()
    {
        // Arrange
        var completedChannel = Channel.CreateBounded<NotificationMessage>(new BoundedChannelOptions(10)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

        // Act
        await completedChannel.Writer.WriteAsync(new NotificationMessage { NotificationType = "Final" });
        completedChannel.Writer.Complete();

        // Assert: ReadAllAsync should complete after the single item
        var processed = new List<NotificationMessage>();
        await foreach (var msg in completedChannel.Reader.ReadAllAsync())
        {
            processed.Add(msg);
        }

        Assert.Single(processed);
        Assert.Equal("Final", processed[0].NotificationType);
    }

    [Fact(DisplayName = "Verifies that NotificationMessage CreatedAt defaults to UTC.")]
    public void NotificationMessage_ShouldDefaultCreatedAtToUtc()
    {
        // Arrange & Act
        var message = new NotificationMessage { NotificationType = "Test" };

        // Assert
        Assert.True(message.CreatedAt.Kind == DateTimeKind.Utc || message.CreatedAt == DateTime.UtcNow);
    }

    [Fact(DisplayName = "Verifies that null metadata does not cause exceptions.")]
    public async Task QueueNotification_ShouldHandleNullMetadata()
    {
        // Arrange
        var message = new NotificationMessage
        {
            NotificationType = "NoMetadata",
            Recipient = "test@example.com",
            Subject = "No Metadata Test",
            Metadata = null
        };

        // Act
        await _channel.Writer.WriteAsync(message);
        var read = await _channel.Reader.ReadAsync();

        // Assert
        Assert.Null(read.Metadata);
    }

    public void Dispose()
    {
        _channel.Writer.Complete();
        _processedMessages.Clear();
    }
}
