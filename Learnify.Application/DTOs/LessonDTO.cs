namespace Learnify.Application.DTOs;

/// <summary>
/// Data transfer object for Lesson entity.
/// </summary>
public class LessonDTO
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? VideoUrl { get; set; }
    public TimeSpan? Duration { get; set; }
    public int Order { get; set; }
    public Guid CourseId { get; set; }
    public string? CourseTitle { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO for creating a new lesson.
/// </summary>
public class CreateLessonDTO
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? VideoUrl { get; set; }
    public TimeSpan? Duration { get; set; }
    public int Order { get; set; }
    public Guid CourseId { get; set; }
}

/// <summary>
/// DTO for updating an existing lesson.
/// </summary>
public class UpdateLessonDTO
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? VideoUrl { get; set; }
    public TimeSpan? Duration { get; set; }
    public int Order { get; set; }
}
