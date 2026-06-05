using Learnify.Application.DTOs;

namespace Learnify.Application.Interfaces;

public interface IAnalyticsService
{
    Task TrackAsync(
        Guid userId,
        string activityType,
        string? entityType = null,
        Guid? entityId = null,
        int? points = null,
        string? metadataJson = null,
        CancellationToken cancellationToken = default);

    Task<DashboardAnalyticsDto> GetDashboardAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<QuizPerformanceDto> GetQuizPerformanceAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecentActivityDto>> GetRecentActivityAsync(
        Guid userId,
        int limit = 20,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AchievementStatusDto>> GetAchievementsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
