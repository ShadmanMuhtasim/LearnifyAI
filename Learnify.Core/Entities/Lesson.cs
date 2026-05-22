namespace Learnify.Core.Entities;

/// <summary>
/// Lesson entity representing lessons within a course.
/// </summary>
public class Lesson : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? VideoUrl { get; set; }
    public TimeSpan? Duration { get; set; }
    public int Order { get; set; }

    public Guid CourseId { get; set; }

    public virtual Course Course { get; set; } = null!;
}
