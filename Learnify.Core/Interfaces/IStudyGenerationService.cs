using Learnify.Core.Models;

namespace Learnify.Core.Interfaces;

public interface IStudyGenerationService
{
    Task<StudyGenerationResult> GenerateSummaryAsync(
        Guid userId,
        string noteId,
        string content,
        string? generationMode,
        CancellationToken cancellationToken = default);

    Task<StudyGenerationResult> GenerateFlashcardsAsync(
        Guid userId,
        string noteId,
        string content,
        int count,
        string? generationMode,
        CancellationToken cancellationToken = default);

    Task<StudyGenerationResult> GenerateStudyTipsAsync(
        Guid userId,
        string topic,
        string? generationMode,
        CancellationToken cancellationToken = default);
}
