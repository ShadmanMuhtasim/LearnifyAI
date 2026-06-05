namespace Learnify.Application.DTOs;

public class StudyPlanItemDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? CourseId { get; set; }
    public string? CourseTitle { get; set; }
    public Guid? NoteId { get; set; }
    public string? NotePreview { get; set; }
    public Guid? QuizId { get; set; }
    public string? QuizTitle { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string PlanType { get; set; } = "Custom";
    public DateTime ScheduledFor { get; set; }
    public int EstimatedMinutes { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string Priority { get; set; } = "Medium";
    public string Source { get; set; } = "Manual";
}

public class CreateStudyPlanItemRequest
{
    public Guid? CourseId { get; set; }
    public Guid? NoteId { get; set; }
    public Guid? QuizId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string PlanType { get; set; } = "Custom";
    public DateTime ScheduledFor { get; set; }
    public int EstimatedMinutes { get; set; } = 30;
    public string Priority { get; set; } = "Medium";
    public string Source { get; set; } = "Manual";
}

public class UpdateStudyPlanItemRequest
{
    public Guid? CourseId { get; set; }
    public Guid? NoteId { get; set; }
    public Guid? QuizId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string PlanType { get; set; } = "Custom";
    public DateTime ScheduledFor { get; set; }
    public int EstimatedMinutes { get; set; } = 30;
    public string Status { get; set; } = "Pending";
    public string Priority { get; set; } = "Medium";
}

public class StudySuggestionDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string PlanType { get; set; } = "Custom";
    public Guid? CourseId { get; set; }
    public Guid? NoteId { get; set; }
    public Guid? QuizId { get; set; }
    public int EstimatedMinutes { get; set; } = 30;
    public string Priority { get; set; } = "Medium";
}

public class StudyPlanSummaryDto
{
    public int PendingCount { get; set; }
    public int CompletedCount { get; set; }
    public int TodayCount { get; set; }
    public int OverdueCount { get; set; }
    public int TotalEstimatedMinutesToday { get; set; }
    public StudyPlanItemDto? NextItem { get; set; }
    public List<StudySuggestionDto> Suggestions { get; set; } = new();
}
