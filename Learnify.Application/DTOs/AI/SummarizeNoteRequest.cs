namespace Learnify.Application.DTOs.AI;

/// <summary>
/// Request DTO for summarizing a note.
/// </summary>
public record SummarizeNoteRequest(
    string NoteId,
    string Content,
    string GenerationMode = "Auto",
    string SummaryDepth = "Balanced",
    bool UseCache = true,
    bool Regenerate = false);
