namespace Learnify.Application.Config;

/// <summary>
/// Configuration settings for Gemini API integration.
/// </summary>
public class GeminiSettings
{
    /// <summary>
    /// The Gemini API key (stored in environment variables or secrets).
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// The Gemini model to use for generation.
    /// </summary>
    public string Model { get; set; } = "gemini-2.0-flash";

    /// <summary>
    /// Maximum number of flashcards to generate.
    /// </summary>
    public int MaxFlashcards { get; set; } = 10;

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}