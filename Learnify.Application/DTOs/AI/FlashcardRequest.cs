namespace Learnify.Application.DTOs.AI;

/// <summary>
/// Request DTO for generating flashcards from educational content.
/// </summary>
public record FlashcardRequest(
    string NoteId,
    string Content,
    int Count = 5,
    string GenerationMode = "Auto",
    bool UseCache = true,
    bool Regenerate = false);
