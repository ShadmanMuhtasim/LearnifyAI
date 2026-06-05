using Learnify.Application.DTOs;

namespace Learnify.Application.Interfaces;

public interface IStudyPlannerService
{
    Task<IReadOnlyList<StudyPlanItemDto>> ListAsync(
        Guid userId,
        DateTime? from,
        DateTime? to,
        string? status,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudyPlanItemDto>> GetUpcomingAsync(
        Guid userId,
        int limit = 5,
        CancellationToken cancellationToken = default);

    Task<StudyPlanSummaryDto> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<StudyPlanItemDto> CreateAsync(
        Guid userId,
        CreateStudyPlanItemRequest request,
        CancellationToken cancellationToken = default);

    Task<StudyPlanItemDto?> UpdateAsync(
        Guid userId,
        Guid id,
        UpdateStudyPlanItemRequest request,
        CancellationToken cancellationToken = default);

    Task<StudyPlanItemDto?> CompleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
}
