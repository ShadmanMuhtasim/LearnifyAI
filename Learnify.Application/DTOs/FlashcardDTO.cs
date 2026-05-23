namespace Learnify.Application.DTOs;

/// <summary>
/// Data transfer object for flashcard Q&A pairs returned from AI generation.
/// </summary>
public class FlashcardDTO
{
    /// <summary>
    /// The question for the flashcard.
    /// </summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// The answer to the question.
    /// </summary>
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// Optional explanation for deeper understanding.
    /// </summary>
    public string? Explanation { get; set; }
}