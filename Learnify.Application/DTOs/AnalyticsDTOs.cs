namespace Learnify.Application.DTOs;

public class RecentActivityDto
{
    public Guid Id { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public int Points { get; set; }
    public DateTime OccurredAt { get; set; }
}

public class DashboardAnalyticsDto
{
    public int TotalCourses { get; set; }
    public int TotalNotes { get; set; }
    public int TotalQuizzes { get; set; }
    public int TotalQuizAttempts { get; set; }
    public decimal AverageQuizScore { get; set; }
    public decimal BestQuizScore { get; set; }
    public int TotalFlashcardsGenerated { get; set; }
    public int TotalSummariesGenerated { get; set; }
    public int TotalStudyTipsGenerated { get; set; }
    public int TotalXp { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int UnlockedAchievements { get; set; }
    public int AvailableAchievements { get; set; }
    public List<RecentActivityDto> RecentActivity { get; set; } = new();
}

public class QuizPerformanceDto
{
    public int AttemptsCount { get; set; }
    public decimal AverageScore { get; set; }
    public decimal BestScore { get; set; }
    public List<RecentQuizAttemptDto> RecentAttempts { get; set; } = new();
    public List<QuizSummaryDto> QuizSummaries { get; set; } = new();
}

public class RecentQuizAttemptDto
{
    public Guid AttemptId { get; set; }
    public Guid QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public int Score { get; set; }
    public int TotalPoints { get; set; }
    public decimal Percentage { get; set; }
    public DateTime CompletedAt { get; set; }
}

public class QuizSummaryDto
{
    public Guid QuizId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int AttemptsCount { get; set; }
    public decimal BestScore { get; set; }
    public decimal AverageScore { get; set; }
}

public class AchievementStatusDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int RequiredValue { get; set; }
    public string AchievementType { get; set; } = string.Empty;
    public int PointsReward { get; set; }
    public bool IsUnlocked { get; set; }
    public DateTime? UnlockedAt { get; set; }
    public int Progress { get; set; }
}
