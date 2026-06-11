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
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class NoteAttachmentMetadataDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FileName
    {
        get => Name;
        set => Name = value;
    }
    public string Type { get; set; } = string.Empty;
    public string ContentType
    {
        get => Type;
        set => Type = value;
    }
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class NoteListItemDTO
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Preview { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public string? CourseTitle { get; set; }
    public string? CourseName
    {
        get => CourseTitle;
        set => CourseTitle = value;
    }
    public bool HasAttachments { get; set; }
    public string ExtractionStatus { get; set; } = "TextReady";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class NoteDetailDTO
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public string? CourseTitle { get; set; }
    public string? CourseName
    {
        get => CourseTitle;
        set => CourseTitle = value;
    }
    public List<string> Tags { get; set; } = new();
    public List<NoteAttachmentMetadataDTO> Attachments { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class PagedResultDTO<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
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
