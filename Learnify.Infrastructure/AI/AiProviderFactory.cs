using Learnify.Core.Interfaces;
using Learnify.Core.Models;
using Learnify.Application.Settings;
using Learnify.Infrastructure.AI.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Learnify.Infrastructure.AI;

/// <summary>
/// Factory that resolves the configured AI provider at runtime.
/// Supports pluggable providers: Gemini, OpenAI, Ollama, Claude.
/// Implements IAiService as a unified facade.
/// </summary>
public class AiProviderFactory : IAiService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AiSettings _aiSettings;
    private readonly ILogger<AiProviderFactory> _logger;
    private IAiProvider? _activeProvider;

    public AiProviderFactory(
        IServiceProvider serviceProvider,
        IOptions<AiSettings> aiSettings,
        ILogger<AiProviderFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _aiSettings = aiSettings.Value;
        _logger = logger;
    }

    private IAiProvider GetActiveProvider()
    {
        if (_activeProvider != null) return _activeProvider;

        var providerName = _aiSettings.ActiveProvider.ToLowerInvariant();

        _activeProvider = providerName switch
        {
            "gemini" => ActivatorUtilities.CreateInstance<GeminiAiProvider>(_serviceProvider),
            "openai" => ActivatorUtilities.CreateInstance<OpenAiProvider>(_serviceProvider),
            "ollama" => ActivatorUtilities.CreateInstance<OllamaAiProvider>(_serviceProvider),
            "claude" => ActivatorUtilities.CreateInstance<ClaudeAiProvider>(_serviceProvider),
            _ => throw new InvalidOperationException(
                $"Unknown AI provider '{_aiSettings.ActiveProvider}'. Supported: Gemini, OpenAI, Ollama, Claude.")
        };

        _logger.LogInformation("AI provider resolved to '{Provider}'", _activeProvider.ProviderName);
        return _activeProvider;
    }

    public async Task<string> SummarizeNoteAsync(string content, CancellationToken ct = default)
    {
        var provider = GetActiveProvider();
        _logger.LogDebug("Summarizing note via '{Provider}'", provider.ProviderName);
        var options = new AiRequestOptions { MaxTokens = 500, Temperature = 0.3f };
        var response = await provider.CompleteAsync(
            $"Summarize the following educational content in 2-3 concise sentences:\n\n{content}",
            options, ct);
        return response;
    }

    public async Task<IReadOnlyList<FlashcardResult>> GenerateFlashcardsAsync(
        string content, int count = 5, CancellationToken ct = default)
    {
        var provider = GetActiveProvider();
        _logger.LogDebug("Generating {Count} flashcards via '{Provider}'", count, provider.ProviderName);
        var prompt = $"Generate exactly {count} flashcard pairs from the following study material. " +
                     $"Each flashcard should have a 'term' and 'definition'. " +
                     $"Return the result as a JSON array of objects with 'term' and 'definition' properties. " +
                     $"Do NOT include any markdown formatting or code blocks — return ONLY the raw JSON array.\n\n" +
                     $"Material:\n{content}";

        var options = new AiRequestOptions { MaxTokens = 2000, Temperature = 0.7f };
        var response = await provider.CompleteAsync(prompt, options, ct);
        return ParseFlashcards(response);
    }

    public async Task<string> GetStudyTipsAsync(string topic, CancellationToken ct = default)
    {
        var provider = GetActiveProvider();
        _logger.LogDebug("Generating study tips for '{Topic}' via '{Provider}'", topic, provider.ProviderName);
        var prompt = $"Provide 3-5 personalized study tips for a student learning about: {topic}. " +
                     $"Focus on active recall, spaced repetition, and concept mastery techniques.";

        var options = new AiRequestOptions { MaxTokens = 500, Temperature = 0.5f };
        var response = await provider.CompleteAsync(prompt, options, ct);
        return response;
    }

    private static List<FlashcardResult> ParseFlashcards(string response)
    {
        try
        {
            var flashcards = System.Text.Json.JsonSerializer.Deserialize<List<FlashcardResult>>(response, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
            return flashcards ?? new List<FlashcardResult>();
        }
        catch
        {
            var flashcards = System.Text.Json.JsonSerializer.Deserialize<List<FlashcardResult>>(response);
            return flashcards ?? new List<FlashcardResult>();
        }
    }
}