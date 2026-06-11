namespace Learnify.Core.Entities;

/// <summary>
/// Represents a file attachment on a note.
/// </summary>
public class NoteAttachment
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Base64 { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid NoteId { get; set; }
    public virtual Note Note { get; set; } = null!;
}

/// <summary>
/// Note entity representing user notes taken on courses.
/// </summary>
public class Note : BaseEntity
{
    public string Content { get; set; } = string.Empty;
    public Guid CourseId { get; set; }

    // Attachment fields (stored inline for simple file attachments)
    public string? AttachmentName { get; set; }
    public string? AttachmentType { get; set; }
    public string? AttachmentBase64 { get; set; }

    public virtual Course Course { get; set; } = null!;
    public virtual ICollection<NoteAttachment> Attachments { get; set; } = new List<NoteAttachment>();
}
