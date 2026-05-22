namespace Learnify.Core.Entities;

/// <summary>
/// Note entity representing user notes taken on courses.
/// </summary>
public class Note : BaseEntity
{
    public string Content { get; set; } = string.Empty;
    public Guid CourseId { get; set; }

    public virtual Course Course { get; set; } = null!;
}