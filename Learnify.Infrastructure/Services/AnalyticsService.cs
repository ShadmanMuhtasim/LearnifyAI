using Learnify.Application.DTOs;
using Learnify.Application.Interfaces;
using Learnify.Core.Entities;
using Learnify.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Learnify.Infrastructure.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(ApplicationDbContext context, ILogger<AnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task TrackAsync(
        Guid userId,
        string activityType,
        string? entityType = null,
        Guid? entityId = null,
        int? points = null,
        string? metadataJson = null,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(activityType))
        {
            return;
        }

        var now = DateTime.UtcNow;
        _context.LearningActivities.Add(new LearningActivity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ActivityType = activityType.Trim(),
            EntityType = string.IsNullOrWhiteSpace(entityType) ? null : entityType.Trim(),
            EntityId = entityId,
            Points = points ?? GetDefaultPoints(activityType),
            OccurredAt = now,
            CreatedAt = now,
            MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? null : metadataJson
        });

        await _context.SaveChangesAsync(cancellationToken);
        await UnlockEligibleAchievementsAsync(userId, cancellationToken);
    }

    public async Task<DashboardAnalyticsDto> GetDashboardAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var attempts = await OwnedAttempts(userId).ToListAsync(cancellationToken);
        var activities = await _context.LearningActivities
            .Where(activity => activity.UserId == userId)
            .OrderByDescending(activity => activity.OccurredAt)
            .ToListAsync(cancellationToken);
        var achievements = await GetAchievementsAsync(userId, cancellationToken);
        var streaks = CalculateStreaks(activities.Select(activity => activity.OccurredAt));

        return new DashboardAnalyticsDto
        {
            TotalCourses = await _context.Courses.CountAsync(course => course.UserId == userId, cancellationToken),
            TotalNotes = await OwnedNotes(userId).CountAsync(cancellationToken),
            TotalQuizzes = await _context.Quizzes.CountAsync(quiz => quiz.UserId == userId, cancellationToken),
            TotalQuizAttempts = attempts.Count,
            AverageQuizScore = attempts.Count == 0 ? 0 : Math.Round(attempts.Average(attempt => attempt.Percentage), 2),
            BestQuizScore = attempts.Count == 0 ? 0 : attempts.Max(attempt => attempt.Percentage),
            TotalFlashcardsGenerated = CountActivity(activities, "FlashcardsGenerated"),
            TotalSummariesGenerated = CountActivity(activities, "SummaryGenerated"),
            TotalStudyTipsGenerated = CountActivity(activities, "StudyTipsGenerated"),
            TotalXp = activities.Sum(activity => activity.Points) + achievements.Where(a => a.IsUnlocked).Sum(a => a.PointsReward),
            CurrentStreak = streaks.Current,
            LongestStreak = streaks.Longest,
            UnlockedAchievements = achievements.Count(achievement => achievement.IsUnlocked),
            AvailableAchievements = achievements.Count,
            RecentActivity = activities.Take(8).Select(ToRecentActivityDto).ToList()
        };
    }

    public async Task<QuizPerformanceDto> GetQuizPerformanceAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var attempts = await OwnedAttempts(userId)
            .OrderByDescending(attempt => attempt.CompletedAt ?? attempt.CreatedAt)
            .ToListAsync(cancellationToken);

        return new QuizPerformanceDto
        {
            AttemptsCount = attempts.Count,
            AverageScore = attempts.Count == 0 ? 0 : Math.Round(attempts.Average(attempt => attempt.Percentage), 2),
            BestScore = attempts.Count == 0 ? 0 : attempts.Max(attempt => attempt.Percentage),
            RecentAttempts = attempts.Take(10).Select(attempt => new RecentQuizAttemptDto
            {
                AttemptId = attempt.Id,
                QuizId = attempt.QuizId,
                QuizTitle = attempt.Quiz.Title,
                Score = attempt.Score,
                TotalPoints = attempt.TotalPoints,
                Percentage = attempt.Percentage,
                CompletedAt = attempt.CompletedAt ?? attempt.CreatedAt
            }).ToList(),
            QuizSummaries = attempts
                .GroupBy(attempt => new { attempt.QuizId, attempt.Quiz.Title })
                .Select(group => new QuizSummaryDto
                {
                    QuizId = group.Key.QuizId,
                    Title = group.Key.Title,
                    AttemptsCount = group.Count(),
                    BestScore = group.Max(attempt => attempt.Percentage),
                    AverageScore = Math.Round(group.Average(attempt => attempt.Percentage), 2)
                })
                .OrderByDescending(summary => summary.BestScore)
                .ToList()
        };
    }

    public async Task<IReadOnlyList<RecentActivityDto>> GetRecentActivityAsync(
        Guid userId,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var activities = await _context.LearningActivities
            .Where(activity => activity.UserId == userId)
            .OrderByDescending(activity => activity.OccurredAt)
            .Take(Math.Clamp(limit, 1, 50))
            .ToListAsync(cancellationToken);

        return activities.Select(ToRecentActivityDto).ToList();
    }

    public async Task<IReadOnlyList<AchievementStatusDto>> GetAchievementsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var achievements = await _context.Achievements
            .Where(achievement => achievement.IsActive)
            .OrderBy(achievement => achievement.Title)
            .ToListAsync(cancellationToken);
        var unlocked = await _context.UserAchievements
            .Where(userAchievement => userAchievement.UserId == userId)
            .ToDictionaryAsync(userAchievement => userAchievement.AchievementId, cancellationToken);
        var progress = await GetProgressSnapshotAsync(userId, cancellationToken);

        return achievements.Select(achievement =>
        {
            unlocked.TryGetValue(achievement.Id, out var userAchievement);
            var value = GetAchievementProgress(achievement, progress);
            return new AchievementStatusDto
            {
                Id = achievement.Id,
                Code = achievement.Code,
                Title = achievement.Title,
                Description = achievement.Description,
                Icon = achievement.Icon,
                RequiredValue = achievement.RequiredValue,
                AchievementType = achievement.AchievementType,
                PointsReward = achievement.PointsReward,
                IsUnlocked = userAchievement != null,
                UnlockedAt = userAchievement?.UnlockedAt,
                Progress = Math.Min(value, achievement.RequiredValue)
            };
        }).ToList();
    }

    private async Task UnlockEligibleAchievementsAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var achievements = await _context.Achievements
                .Where(achievement => achievement.IsActive)
                .ToListAsync(cancellationToken);
            var unlockedIds = await _context.UserAchievements
                .Where(userAchievement => userAchievement.UserId == userId)
                .Select(userAchievement => userAchievement.AchievementId)
                .ToListAsync(cancellationToken);
            var unlockedSet = unlockedIds.ToHashSet();
            var progress = await GetProgressSnapshotAsync(userId, cancellationToken);
            var now = DateTime.UtcNow;

            foreach (var achievement in achievements)
            {
                if (unlockedSet.Contains(achievement.Id) ||
                    GetAchievementProgress(achievement, progress) < achievement.RequiredValue)
                {
                    continue;
                }

                _context.UserAchievements.Add(new UserAchievement
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    AchievementId = achievement.Id,
                    UnlockedAt = now,
                    CreatedAt = now
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Achievement unlock check failed for user {UserId}", userId);
        }
    }

    private async Task<ProgressSnapshot> GetProgressSnapshotAsync(Guid userId, CancellationToken cancellationToken)
    {
        var activities = await _context.LearningActivities
            .Where(activity => activity.UserId == userId)
            .ToListAsync(cancellationToken);
        var streaks = CalculateStreaks(activities.Select(activity => activity.OccurredAt));

        return new ProgressSnapshot
        {
            Courses = await _context.Courses.CountAsync(course => course.UserId == userId, cancellationToken),
            Notes = await OwnedNotes(userId).CountAsync(cancellationToken),
            Quizzes = await _context.Quizzes.CountAsync(quiz => quiz.UserId == userId, cancellationToken),
            PerfectQuizAttempts = await OwnedAttempts(userId).CountAsync(attempt => attempt.Percentage >= 100, cancellationToken),
            FlashcardsGenerated = CountActivity(activities, "FlashcardsGenerated"),
            ActivityCount = activities.Count,
            LongestStreak = streaks.Longest,
            AiToolKindsUsed = new[]
            {
                activities.Any(activity => activity.ActivityType == "SummaryGenerated"),
                activities.Any(activity => activity.ActivityType == "FlashcardsGenerated"),
                activities.Any(activity => activity.ActivityType == "StudyTipsGenerated")
            }.Count(used => used)
        };
    }

    private IQueryable<Note> OwnedNotes(Guid userId) =>
        _context.Notes.Where(note => note.Course.UserId == userId);

    private IQueryable<QuizAttempt> OwnedAttempts(Guid userId) =>
        _context.QuizAttempts
            .Include(attempt => attempt.Quiz)
            .Where(attempt => attempt.UserId == userId && attempt.Quiz.UserId == userId);

    private static RecentActivityDto ToRecentActivityDto(LearningActivity activity) => new()
    {
        Id = activity.Id,
        ActivityType = activity.ActivityType,
        EntityType = activity.EntityType,
        EntityId = activity.EntityId,
        Points = activity.Points,
        OccurredAt = activity.OccurredAt
    };

    private static int CountActivity(IEnumerable<LearningActivity> activities, string activityType) =>
        activities.Count(activity => activity.ActivityType == activityType);

    private static int GetDefaultPoints(string activityType) =>
        activityType switch
        {
            "CourseCreated" => 10,
            "NoteUploaded" => 10,
            "PdfUploaded" => 10,
            "SummaryGenerated" => 5,
            "FlashcardsGenerated" => 10,
            "StudyTipsGenerated" => 5,
            "QuizGenerated" => 15,
            "QuizAttemptSubmitted" => 20,
            _ => 5
        };

    private static int GetAchievementProgress(Achievement achievement, ProgressSnapshot progress) =>
        achievement.AchievementType switch
        {
            "CourseCreated" => progress.Courses,
            "NoteUploaded" => progress.Notes,
            "QuizGenerated" => progress.Quizzes,
            "PerfectQuiz" => progress.PerfectQuizAttempts,
            "FlashcardsGenerated" => progress.FlashcardsGenerated,
            "AiToolKindsUsed" => progress.AiToolKindsUsed,
            "LongestStreak" => progress.LongestStreak,
            "ActivityCount" => progress.ActivityCount,
            _ => 0
        };

    private static (int Current, int Longest) CalculateStreaks(IEnumerable<DateTime> activityTimes)
    {
        var days = activityTimes
            .Select(time => time.Date)
            .Distinct()
            .OrderByDescending(day => day)
            .ToList();
        if (days.Count == 0)
        {
            return (0, 0);
        }

        var today = DateTime.UtcNow.Date;
        var current = 0;
        var cursor = days.Contains(today) ? today : today.AddDays(-1);
        while (days.Contains(cursor))
        {
            current++;
            cursor = cursor.AddDays(-1);
        }

        var longest = 1;
        var run = 1;
        for (var index = 1; index < days.Count; index++)
        {
            if ((days[index - 1] - days[index]).TotalDays == 1)
            {
                run++;
                longest = Math.Max(longest, run);
            }
            else
            {
                run = 1;
            }
        }

        return (current, longest);
    }

    private sealed class ProgressSnapshot
    {
        public int Courses { get; set; }
        public int Notes { get; set; }
        public int Quizzes { get; set; }
        public int PerfectQuizAttempts { get; set; }
        public int FlashcardsGenerated { get; set; }
        public int AiToolKindsUsed { get; set; }
        public int LongestStreak { get; set; }
        public int ActivityCount { get; set; }
    }
}
