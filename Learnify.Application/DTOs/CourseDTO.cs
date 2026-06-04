namespace Learnify.Application.DTOs;

public class CourseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Data transfer object for Course entity.
/// </summary>
public class CourseDTO
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string? InstructorName { get; set; }
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO for creating a new course.
/// </summary>
public class CreateCourseDTO
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid UserId { get; set; }
}

/// <summary>
/// DTO for updating an existing course.
/// </summary>
public class UpdateCourseDTO
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
