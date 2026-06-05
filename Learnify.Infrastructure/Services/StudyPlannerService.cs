using Learnify.Application.DTOs;
using Learnify.Application.Interfaces;
using Learnify.Core.Entities;
using Learnify.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Learnify.Infrastructure.Services;

public class StudyPlannerService : IStudyPlannerService
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pending",
        "Completed",
        "Skipped"
    };

    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<StudyPlannerService> _logger;

    public StudyPlannerService(ApplicationDbContext dbContext, ILogger<StudyPlannerService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<StudyPlanItemDto>> ListAsync(
        Guid userId,
        DateTime? from,
        DateTime? to,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = IncludePlannerLinks(_dbContext.StudyPlanItems)
            .Where(item => item.UserId == userId);

        if (from.HasValue)
        {
            query = query.Where(item => item.ScheduledFor >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(item => item.ScheduledFor <= to.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(item => item.Status == NormalizeStatus(status));
        }

        var items = await query
            .OrderBy(item => item.ScheduledFor)
            .ToListAsync(cancellationToken);

        return items.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<StudyPlanItemDto>> GetUpcomingAsync(
        Guid userId,
        int limit = 5,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var items = await IncludePlannerLinks(_dbContext.StudyPlanItems)
            .Where(item => item.UserId == userId && item.Status == "Pending" && item.ScheduledFor >= now)
            .OrderBy(item => item.ScheduledFor)
            .Take(Math.Clamp(limit, 1, 20))
            .ToListAsync(cancellationToken);

        return items.Select(ToDto).ToList();
    }

    public async Task<StudyPlanSummaryDto> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var tomorrowStart = todayStart.AddDays(1);
        var items = await IncludePlannerLinks(_dbContext.StudyPlanItems)
            .Where(item => item.UserId == userId)
            .ToListAsync(cancellationToken);

        var pending = items.Where(item => item.Status == "Pending").ToList();
        var todayItems = items
            .Where(item => item.ScheduledFor >= todayStart && item.ScheduledFor < tomorrowStart)
            .ToList();

        return new StudyPlanSummaryDto
        {
            PendingCount = pending.Count,
            CompletedCount = items.Count(item => item.Status == "Completed"),
            TodayCount = todayItems.Count,
            OverdueCount = pending.Count(item => item.ScheduledFor < now),
            TotalEstimatedMinutesToday = todayItems.Sum(item => item.EstimatedMinutes),
            NextItem = pending
                .Where(item => item.ScheduledFor >= now)
                .OrderBy(item => item.ScheduledFor)
                .Select(ToDto)
                .FirstOrDefault(),
            Suggestions = await BuildSuggestionsAsync(userId, pending, cancellationToken)
        };
    }

    public async Task<StudyPlanItemDto> CreateAsync(
        Guid userId,
        CreateStudyPlanItemRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request.Title, request.EstimatedMinutes, request.PlanType, request.Priority);
        await ValidateOwnedReferencesAsync(userId, request.CourseId, request.NoteId, request.QuizId, cancellationToken);

        var item = new StudyPlanItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CourseId = request.CourseId,
            NoteId = request.NoteId,
            QuizId = request.QuizId,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            PlanType = NormalizeText(request.PlanType, "Custom"),
            ScheduledFor = request.ScheduledFor == default ? DateTime.UtcNow : request.ScheduledFor,
            EstimatedMinutes = request.EstimatedMinutes,
            Status = "Pending",
            Priority = NormalizeText(request.Priority, "Medium"),
            Source = NormalizeText(request.Source, "Manual"),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.StudyPlanItems.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await LoadDtoAsync(userId, item.Id, cancellationToken)
            ?? throw new InvalidOperationException("Unable to load created study plan item.");
    }

    public async Task<StudyPlanItemDto?> UpdateAsync(
        Guid userId,
        Guid id,
        UpdateStudyPlanItemRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request.Title, request.EstimatedMinutes, request.PlanType, request.Priority);
        var status = NormalizeStatus(request.Status);
        await ValidateOwnedReferencesAsync(userId, request.CourseId, request.NoteId, request.QuizId, cancellationToken);

        var item = await _dbContext.StudyPlanItems
            .FirstOrDefaultAsync(studyItem => studyItem.Id == id && studyItem.UserId == userId, cancellationToken);

        if (item == null)
        {
            return null;
        }

        item.CourseId = request.CourseId;
        item.NoteId = request.NoteId;
        item.QuizId = request.QuizId;
        item.Title = request.Title.Trim();
        item.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        item.PlanType = NormalizeText(request.PlanType, "Custom");
        item.ScheduledFor = request.ScheduledFor == default ? item.ScheduledFor : request.ScheduledFor;
        item.EstimatedMinutes = request.EstimatedMinutes;
        item.Priority = NormalizeText(request.Priority, "Medium");
        item.Status = status;
        item.CompletedAt = status == "Completed" ? item.CompletedAt ?? DateTime.UtcNow : null;
        item.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await LoadDtoAsync(userId, id, cancellationToken);
    }

    public async Task<StudyPlanItemDto?> CompleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.StudyPlanItems
            .FirstOrDefaultAsync(studyItem => studyItem.Id == id && studyItem.UserId == userId, cancellationToken);

        if (item == null)
        {
            return null;
        }

        item.Status = "Completed";
        item.CompletedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Study plan item {StudyPlanItemId} completed for user {UserId}.", id, userId);

        return await LoadDtoAsync(userId, id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.StudyPlanItems
            .FirstOrDefaultAsync(studyItem => studyItem.Id == id && studyItem.UserId == userId, cancellationToken);

        if (item == null)
        {
            return false;
        }

        _dbContext.StudyPlanItems.Remove(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<StudyPlanItemDto?> LoadDtoAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var item = await IncludePlannerLinks(_dbContext.StudyPlanItems)
            .FirstOrDefaultAsync(studyItem => studyItem.Id == id && studyItem.UserId == userId, cancellationToken);

        return item == null ? null : ToDto(item);
    }

    private async Task ValidateOwnedReferencesAsync(
        Guid userId,
        Guid? courseId,
        Guid? noteId,
        Guid? quizId,
        CancellationToken cancellationToken)
    {
        if (courseId.HasValue)
        {
            var ownsCourse = await _dbContext.Courses
                .AnyAsync(course => course.Id == courseId.Value && course.UserId == userId, cancellationToken);
            if (!ownsCourse)
            {
                throw new InvalidOperationException("Course not found.");
            }
        }

        if (noteId.HasValue)
        {
            var note = await _dbContext.Notes
                .Include(note => note.Course)
                .FirstOrDefaultAsync(note => note.Id == noteId.Value, cancellationToken);

            if (note?.Course.UserId != userId)
            {
                throw new InvalidOperationException("Note not found.");
            }

            if (courseId.HasValue && note.CourseId != courseId.Value)
            {
                throw new InvalidOperationException("Note does not belong to the selected course.");
            }
        }

        if (quizId.HasValue)
        {
            var quiz = await _dbContext.Quizzes
                .FirstOrDefaultAsync(quiz => quiz.Id == quizId.Value, cancellationToken);

            if (quiz?.UserId != userId)
            {
                throw new InvalidOperationException("Quiz not found.");
            }

            if (courseId.HasValue && quiz.CourseId.HasValue && quiz.CourseId != courseId.Value)
            {
                throw new InvalidOperationException("Quiz does not belong to the selected course.");
            }

            if (noteId.HasValue && quiz.NoteId.HasValue && quiz.NoteId != noteId.Value)
            {
                throw new InvalidOperationException("Quiz does not belong to the selected note.");
            }
        }
    }

    private async Task<List<StudySuggestionDto>> BuildSuggestionsAsync(
        Guid userId,
        List<StudyPlanItem> pendingItems,
        CancellationToken cancellationToken)
    {
        var suggestions = new List<StudySuggestionDto>();

        if (pendingItems.Count > 0)
        {
            var nextPending = pendingItems.OrderBy(item => item.ScheduledFor).First();
            suggestions.Add(new StudySuggestionDto
            {
                Title = "Focus on your next pending task",
                Description = nextPending.Title,
                PlanType = nextPending.PlanType,
                CourseId = nextPending.CourseId,
                NoteId = nextPending.NoteId,
                QuizId = nextPending.QuizId,
                EstimatedMinutes = Math.Max(nextPending.EstimatedMinutes, 15),
                Priority = nextPending.Priority
            });
        }

        var latestNote = await _dbContext.Notes
            .Include(note => note.Course)
            .Where(note => note.Course.UserId == userId)
            .OrderByDescending(note => note.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestNote != null)
        {
            suggestions.Add(new StudySuggestionDto
            {
                Title = "Review latest note",
                Description = Preview(latestNote.Content, 140),
                PlanType = "ReviewNote",
                CourseId = latestNote.CourseId,
                NoteId = latestNote.Id,
                EstimatedMinutes = 25,
                Priority = "Medium"
            });

            var hasRecentAttempt = await _dbContext.QuizAttempts
                .AnyAsync(attempt => attempt.UserId == userId && attempt.CreatedAt >= DateTime.UtcNow.AddDays(-7), cancellationToken);

            if (!hasRecentAttempt)
            {
                suggestions.Add(new StudySuggestionDto
                {
                    Title = "Generate or take a quiz",
                    Description = "Use your recent notes to check recall.",
                    PlanType = "TakeQuiz",
                    CourseId = latestNote.CourseId,
                    NoteId = latestNote.Id,
                    EstimatedMinutes = 20,
                    Priority = "High"
                });
            }
        }

        var weakAttempt = await _dbContext.QuizAttempts
            .Include(attempt => attempt.Quiz)
            .Where(attempt => attempt.UserId == userId && attempt.Percentage < 70)
            .OrderByDescending(attempt => attempt.CompletedAt ?? attempt.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (weakAttempt != null)
        {
            suggestions.Add(new StudySuggestionDto
            {
                Title = "Review weak quiz",
                Description = $"Last score: {weakAttempt.Percentage:0}%. Revisit the linked material before another attempt.",
                PlanType = "TakeQuiz",
                CourseId = weakAttempt.Quiz.CourseId,
                NoteId = weakAttempt.Quiz.NoteId,
                QuizId = weakAttempt.QuizId,
                EstimatedMinutes = 30,
                Priority = "High"
            });
        }

        return suggestions
            .GroupBy(suggestion => suggestion.Title)
            .Select(group => group.First())
            .Take(4)
            .ToList();
    }

    private static IQueryable<StudyPlanItem> IncludePlannerLinks(IQueryable<StudyPlanItem> query)
        => query
            .Include(item => item.Course)
            .Include(item => item.Note)
            .Include(item => item.Quiz);

    private static StudyPlanItemDto ToDto(StudyPlanItem item)
        => new()
        {
            Id = item.Id,
            UserId = item.UserId,
            CourseId = item.CourseId,
            CourseTitle = item.Course?.Title,
            NoteId = item.NoteId,
            NotePreview = item.Note == null ? null : Preview(item.Note.Content, 120),
            QuizId = item.QuizId,
            QuizTitle = item.Quiz?.Title,
            Title = item.Title,
            Description = item.Description,
            PlanType = item.PlanType,
            ScheduledFor = item.ScheduledFor,
            EstimatedMinutes = item.EstimatedMinutes,
            Status = item.Status,
            CompletedAt = item.CompletedAt,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
            Priority = item.Priority,
            Source = item.Source
        };

    private static void ValidateRequest(string title, int estimatedMinutes, string planType, string priority)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("Title is required.");
        }

        if (estimatedMinutes <= 0 || estimatedMinutes > 600)
        {
            throw new InvalidOperationException("Estimated minutes must be between 1 and 600.");
        }

        if (string.IsNullOrWhiteSpace(planType))
        {
            throw new InvalidOperationException("Plan type is required.");
        }

        if (string.IsNullOrWhiteSpace(priority))
        {
            throw new InvalidOperationException("Priority is required.");
        }
    }

    private static string NormalizeStatus(string? status)
    {
        var value = NormalizeText(status, "Pending");
        if (!ValidStatuses.Contains(value))
        {
            throw new InvalidOperationException("Status must be Pending, Completed, or Skipped.");
        }

        return string.Equals(value, "completed", StringComparison.OrdinalIgnoreCase) ? "Completed"
            : string.Equals(value, "skipped", StringComparison.OrdinalIgnoreCase) ? "Skipped"
            : "Pending";
    }

    private static string NormalizeText(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string Preview(string content, int maxLength)
    {
        var compact = string.Join(" ", content.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        return compact.Length <= maxLength ? compact : compact[..maxLength] + "...";
    }
}
