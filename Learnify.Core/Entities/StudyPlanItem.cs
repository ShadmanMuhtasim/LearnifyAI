namespace Learnify.Core.Entities;

/// <summary>
/// A per-user scheduled study task linked to optional learning material.
/// </summary>
public class StudyPlanItem : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? CourseId { get; set; }
    public Guid? NoteId { get; set; }
    public Guid? QuizId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string PlanType { get; set; } = "Custom";
    public DateTime ScheduledFor { get; set; } = DateTime.UtcNow;
    public int EstimatedMinutes { get; set; } = 30;
    public string Status { get; set; } = "Pending";
    public DateTime? CompletedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string Priority { get; set; } = "Medium";
    public string Source { get; set; } = "Manual";

    public virtual User User { get; set; } = null!;
    public virtual Course? Course { get; set; }
    public virtual Note? Note { get; set; }
    public virtual Quiz? Quiz { get; set; }
}
