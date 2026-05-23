namespace Learnify.Application.DTOs.AI;

/// <summary>
/// Response DTO for note summarization.
/// </summary>
public record SummarizeNoteResponse(
    string NoteId,
    string Summary,
    DateTime GeneratedAt);