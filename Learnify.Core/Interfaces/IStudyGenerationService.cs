using Learnify.Core.Models;

namespace Learnify.Core.Interfaces;

public interface IStudyGenerationService
{
    Task<StudyGenerationResult<string>> GenerateSummaryAsync(
        Guid userId,
        string content,
        string generationMode,
        string summaryDepth,
        bool useCache,
        bool regenerate,
        CancellationToken cancellationToken = default);

    Task<StudyGenerationResult<IReadOnlyList<FlashcardResult>>> GenerateFlashcardsAsync(
        Guid userId,
        string content,
        int count,
        string generationMode,
        bool useCache,
        bool regenerate,
        CancellationToken cancellationToken = default);

    Task<StudyGenerationResult<string>> GenerateStudyTipsAsync(
        Guid userId,
        string content,
        string generationMode,
        bool useCache,
        bool regenerate,
        CancellationToken cancellationToken = default);
}
