namespace Learnify.Core.Entities;

/// <summary>
/// Course entity representing educational courses created by instructors.
/// </summary>
public class Course : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual ICollection<Note> Notes { get; set; } = new List<Note>();
}