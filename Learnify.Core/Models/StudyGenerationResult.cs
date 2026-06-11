namespace Learnify.Core.Models;

public record StudyGenerationResult(
    string? NoteId,
    string? Summary,
    IReadOnlyList<FlashcardResult>? Flashcards,
    string? Tips,
    DateTime GeneratedAt,
    string GenerationModeUsed,
    string? ProviderUsed,
    string? Notice,
    bool FromCache);
