namespace Learnify.Application.DTOs;

/// <summary>
/// Represents a file attachment.
/// </summary>
public class NoteAttachmentDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Base64 { get; set; } = string.Empty;
}

/// <summary>
/// Data transfer object for Note entity.
/// </summary>
public class NoteDTO
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public string? CourseTitle { get; set; }
    public List<NoteAttachmentDTO> Attachments { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for creating a new note.
/// </summary>
public class CreateNoteDTO
{
    public string Content { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public List<NoteAttachmentDTO> Attachments { get; set; } = new();
}

/// <summary>
/// DTO for updating an existing note.
/// </summary>
public class UpdateNoteDTO
{
    public string Content { get; set; } = string.Empty;
    public List<NoteAttachmentDTO> Attachments { get; set; } = new();
}
