using Learnify.Core.Models;

namespace Learnify.Core.Interfaces;

/// <summary>
/// Contract for AI provider implementations.
/// All AI providers (Gemini, OpenAI, Ollama, Claude, etc.) must implement this interface
/// to maintain a provider-agnostic architecture via the strategy pattern.
/// </summary>
public interface IAiProvider
{
    /// <summary>
    /// Gets the name of the AI provider (e.g., "Gemini", "OpenAI", "Ollama", "Claude").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Sends a prompt to the AI provider and returns the generated response.
    /// </summary>
    /// <param name="prompt">The prompt text to send to the AI model.</param>
    /// <param name="options">Configuration options for the AI request (temperature, max tokens, etc.).</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>The generated response text from the AI provider.</returns>
    Task<string> CompleteAsync(
        string prompt,
        AiRequestOptions options,
        CancellationToken ct);
}