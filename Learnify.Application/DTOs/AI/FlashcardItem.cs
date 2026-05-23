namespace Learnify.Application.DTOs.AI;

/// <summary>
/// Represents a single flashcard item with a question and answer.
/// </summary>
public record FlashcardItem(
    string Question,
    string Answer);