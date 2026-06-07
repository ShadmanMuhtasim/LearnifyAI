namespace Learnify.Application.DTOs.AI;

/// <summary>
/// Response DTO for flashcard generation.
/// </summary>
public record FlashcardResponse(
    string NoteId,
    IReadOnlyList<FlashcardItem> Flashcards,
    DateTime GeneratedAt,
    string GenerationModeUsed = "AIProvider",
    string? ProviderUsed = null,
    bool FromCache = false,
    string? Notice = null,
    string? ErrorCode = null);
