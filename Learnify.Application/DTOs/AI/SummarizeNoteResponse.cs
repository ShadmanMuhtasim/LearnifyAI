namespace Learnify.Application.DTOs.AI;

/// <summary>
/// Response DTO for note summarization.
/// </summary>
public record SummarizeNoteResponse(
    string NoteId,
    string Summary,
    DateTime GeneratedAt,
    string GenerationModeUsed = "AIProvider",
    string? ProviderUsed = null,
    bool FromCache = false,
    string? Notice = null,
    string? ErrorCode = null);
