namespace Learnify.Application.DTOs;

/// <summary>
/// Data transfer object for Note entity.
/// </summary>
public class NoteDTO
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public string? CourseTitle { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for creating a new note.
/// </summary>
public class CreateNoteDTO
{
    public string Content { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
}

/// <summary>
/// DTO for updating an existing note.
/// </summary>
public class UpdateNoteDTO
{
    public string Content { get; set; } = string.Empty;
}