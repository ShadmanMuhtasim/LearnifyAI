namespace Learnify.Application.DTOs.AI;

/// <summary>
/// Response DTO for flashcard generation.
/// </summary>
public record FlashcardResponse(
    string NoteId,
    IReadOnlyList<FlashcardItem> Flashcards,
    DateTime GeneratedAt,
    string? GenerationModeUsed = null,
    string? ProviderUsed = null,
    string? Notice = null,
    bool FromCache = false);
