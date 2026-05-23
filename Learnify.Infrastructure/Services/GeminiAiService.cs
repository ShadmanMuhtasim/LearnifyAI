using Learnify.Application.Config;
using Learnify.Core.Interfaces;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Learnify.Infrastructure.Services;

/// <summary>
/// Gemini API implementation for AI-powered features.
/// Handles note summarization, flashcard generation, and recommendations.
/// </summary>
public class GeminiAiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiAiService> _logger;

    public GeminiAiService(IHttpClientFactory httpClientFactory, IOptions<GeminiSettings> settings, ILogger<GeminiAiService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("GeminiApi");
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> SummarizeNotesAsync(string notes, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(notes))
            throw new ArgumentException("Notes cannot be null or empty.", nameof(notes));

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            throw new InvalidOperationException("Gemini API key is not configured.");

        var prompt = @"You are a helpful AI tutor. Summarize the following student notes into a clear, concise, structured format.
Keep key concepts, definitions, and important details. Use bullet points and headings where appropriate.

Student Notes:
""" + notes + @"""

Summary:";

        return await CallGeminiApiAsync(prompt, cancellationToken);
    }

    public async Task<List<Flashcard>> GenerateFlashcardsAsync(string content, string title, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be null or empty.", nameof(content));

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            throw new InvalidOperationException("Gemini API key is not configured.");

        var prompt = @$"You are a helpful AI tutor. Generate flashcard Q&A pairs from the following lesson content.
Return exactly {_settings.MaxFlashcards} flashcards in JSON format as an array of objects with ""question"", ""answer"", and ""explanation"" properties.

Lesson Title: {title}

Lesson Content:
{content}

Return ONLY a valid JSON array. Example format:
[{{""question"": ""..."", ""answer"": ""..."", ""explanation"": ""...""}}]";

        var response = await CallGeminiApiAsync(prompt, cancellationToken);

        return ParseFlashcardsFromResponse(response);
    }

    public async Task<List<string>> GenerateRecommendationsAsync(string courseId, string courseTitle, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(courseTitle))
            throw new ArgumentException("Course title cannot be null or empty.", nameof(courseTitle));

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            throw new InvalidOperationException("Gemini API key is not configured.");

        var prompt = @$"You are a helpful AI tutor. Recommend 5 additional learning resources or topics for a student studying:

Course Title: {courseTitle}
Course ID: {courseId}

Return ONLY a JSON array of strings, one per recommendation. Example format:
[""Recommendation 1"", ""Recommendation 2"", ...]";

        var response = await CallGeminiApiAsync(prompt, cancellationToken);

        return ParseRecommendationsFromResponse(response);
    }

    private async Task<string> CallGeminiApiAsync(string prompt, CancellationToken cancellationToken)
    {
        var apiUrl = $"/v1beta/models/{_settings.Model}:generateContent?key={_settings.ApiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.7,
                topK = 40,
                topP = 0.95,
                maxOutputTokens = 2048,
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

        try
        {
            var response = await _httpClient.PostAsync(apiUrl, content, timeoutCts.Token);

            // Handle rate limiting (429)
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Gemini API rate limit hit. Retrying after delay...");
                await Task.Delay(5000, cancellationToken);
                response = await _httpClient.PostAsync(apiUrl, content, cancellationToken);
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini API error ({StatusCode}): {ErrorBody}", response.StatusCode, errorBody);
                throw new InvalidOperationException($"Gemini API error: {response.StatusCode} - {errorBody}");
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return ExtractTextFromResponse(responseBody);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            _logger.LogError("Gemini API request timed out after {Timeout} seconds", _settings.TimeoutSeconds);
            throw new TimeoutException($"Gemini API request timed out after {_settings.TimeoutSeconds} seconds.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Gemini API request was cancelled.");
            throw;
        }
    }

    private string ExtractTextFromResponse(string responseBody)
    {
        try
        {
            var doc = System.Text.Json.JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            // Navigate to candidates[0].content.parts[0].text
            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var candidate = candidates[0];
                if (candidate.TryGetProperty("content", out var content) &&
                    content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                {
                    var textElement = parts[0];
                    if (textElement.TryGetProperty("text", out var textProp))
                    {
                        return textProp.GetString() ?? string.Empty;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Gemini API response.");
        }

        return responseBody;
    }

    private List<Flashcard> ParseFlashcardsFromResponse(string response)
    {
        var flashcards = new List<Flashcard>();

        try
        {
            // Try to extract JSON array from the response (in case Gemini wraps it in text)
            var jsonStart = response.IndexOf('[');
            var jsonEnd = response.LastIndexOf(']');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var cards = System.Text.Json.JsonSerializer.Deserialize<List<Flashcard>>(json);
                return cards ?? new List<Flashcard>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse flashcards from Gemini response. Raw response: {Response}", response);
        }

        // Fallback: create a single flashcard from the raw response
        flashcards.Add(new Flashcard
        {
            Question = "Key concepts from the lesson",
            Answer = response.Trim(),
            Explanation = "Generated by Gemini AI"
        });

        return flashcards;
    }

    private List<string> ParseRecommendationsFromResponse(string response)
    {
        try
        {
            var recommendations = System.Text.Json.JsonSerializer.Deserialize<List<string>>(response);
            return recommendations ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse recommendations from Gemini response.");
            // Fallback: return a single recommendation
            return [$"Additional resources for: {response.Trim().Split('\n').FirstOrDefault() ?? "this course"}"];
        }
    }
}