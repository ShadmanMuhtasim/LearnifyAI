using Learnify.Core.Models;

namespace Learnify.Core.Interfaces;

/// <summary>
/// High-level AI service used by controllers and background workers.
/// Provides educational AI features: note summarization, flashcard generation, and study tips.
/// </summary>
public interface IAiService
{
    /// <summary>
    /// Summarizes the given note content in a concise, educational summary.
    /// </summary>
    /// <param name="content">The note content to summarize.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A concise summary of the note content.</returns>
    Task<string> SummarizeNoteAsync(string content, CancellationToken ct = default);

    /// <summary>
    /// Generates flashcards from the given educational content.
    /// </summary>
    /// <param name="content">The content to generate flashcards from.</param>
    /// <param name="count">Number of flashcards to generate (default 5).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of flashcard question-answer pairs.</returns>
    Task<IReadOnlyList<FlashcardResult>> GenerateFlashcardsAsync(
        string content,
        int count = 5,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a structured quiz from educational content.
    /// </summary>
    Task<GeneratedQuizResult> GenerateQuizAsync(
        string content,
        IReadOnlyList<string> questionTypes,
        string difficulty,
        int numberOfQuestions,
        CancellationToken ct = default);

    /// <summary>
    /// Generates personalized study tips for the given topic.
    /// </summary>
    /// <param name="topic">The topic to generate study tips for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Bullet-point study tips as a formatted string.</returns>
    Task<string> GetStudyTipsAsync(string topic, CancellationToken ct = default);

    /// <summary>
    /// Analyzes an uploaded document and returns a structured note summary.
    /// </summary>
    /// <param name="content">Text or derived content sent to the AI provider.</param>
    /// <param name="fileName">Original file name for context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Structured note analysis metadata.</returns>
    Task<NoteAnalysisResult> AnalyzeDocumentAsync(string content, string fileName, CancellationToken ct = default);
}
