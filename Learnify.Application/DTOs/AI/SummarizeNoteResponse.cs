namespace Learnify.Application.DTOs.AI;

/// <summary>
/// Response DTO for note summarization.
/// </summary>
public record SummarizeNoteResponse(
    string NoteId,
    string Summary,
    DateTime GeneratedAt,
    string? GenerationModeUsed = null,
    string? ProviderUsed = null,
    string? Notice = null,
    bool FromCache = false);
