using Learnify.Application.Settings;
using Learnify.Core.Interfaces;
using Learnify.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Learnify.Infrastructure.AI.Providers;

/// <summary>
/// Google Gemini AI provider implementation using the free tier REST API.
/// </summary>
public class GeminiAiProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly AiSettings.GeminiSettings _geminiSettings;
    private readonly ILogger<GeminiAiProvider> _logger;

    public string ProviderName => "Gemini";

    public GeminiAiProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<AiSettings> aiSettings,
        ILogger<GeminiAiProvider> logger)
    {
        _httpClient = httpClientFactory.CreateClient("GeminiClient");
        _geminiSettings = aiSettings.Value.Gemini;
        _logger = logger;
    }

    public async Task<string> CompleteAsync(
        string prompt,
        AiRequestOptions options,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_geminiSettings.ApiKey))
        {
            throw new AiProviderException(
                "AI_CONFIG_MISSING",
                "Gemini API key is not configured. Add it in AI Settings or switch to Free Local mode.",
                ProviderName,
                400);
        }

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
                temperature = options.Temperature,
                maxOutputTokens = options.MaxTokens
            }
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{_geminiSettings.Model}:generateContent?key={_geminiSettings.ApiKey}";

        var response = await _httpClient.PostAsync(requestUrl, jsonContent, ct);

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            var safeMessage = TryReadGeminiErrorMessage(responseJson);
            var isRateLimit = (int)response.StatusCode == 429 || IsQuotaOrRateLimit(safeMessage);
            throw new AiProviderException(
                isRateLimit ? "AI_RATE_LIMIT" : "AI_PROVIDER_UNAVAILABLE",
                isRateLimit
                    ? "Gemini quota or rate limit was reached. You can wait, switch to Free Local mode, add your own API key, or use your local LLM server."
                    : "Gemini provider is unavailable. Check provider settings or switch to Free Local mode.",
                ProviderName,
                isRateLimit ? 429 : 502,
                safeMessage);
        }

        try
        {
            var jsonDoc = JsonDocument.Parse(responseJson);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("candidates", out var candidates)
                && candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                if (firstCandidate.TryGetProperty("content", out var content)
                    && content.TryGetProperty("parts", out var parts)
                    && parts[0].TryGetProperty("text", out var textElement))
                {
                    var text = textElement.GetString();
                    _logger.LogDebug("Gemini response received for model {Model}", _geminiSettings.Model);
                    return text?.Trim() ?? string.Empty;
                }
            }

            // Try to get an error message from the response
            if (root.TryGetProperty("error", out var error)
                && error.TryGetProperty("message", out var errorMessage))
            {
                var safeMessage = errorMessage.GetString() ?? "Gemini API returned an error.";
                throw new AiProviderException(
                    IsQuotaOrRateLimit(safeMessage) ? "AI_RATE_LIMIT" : "AI_PROVIDER_UNAVAILABLE",
                    IsQuotaOrRateLimit(safeMessage)
                        ? "Gemini quota or rate limit was reached. You can wait, switch to Free Local mode, add your own API key, or use your local LLM server."
                        : "Gemini provider is unavailable. Check provider settings or switch to Free Local mode.",
                    ProviderName,
                    IsQuotaOrRateLimit(safeMessage) ? 429 : 502,
                    safeMessage);
            }

            _logger.LogWarning("Could not parse Gemini response for model {Model}", _geminiSettings.Model);
            throw new AiProviderException(
                "AI_PROVIDER_UNAVAILABLE",
                "Gemini returned an unreadable response. Try again or switch to Free Local mode.",
                ProviderName,
                502);
        }
        catch (AiProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Gemini response");
            throw new AiProviderException(
                "AI_PROVIDER_UNAVAILABLE",
                "Gemini provider is unavailable. Check provider settings or switch to Free Local mode.",
                ProviderName,
                502,
                ex.Message,
                ex);
        }
    }

    private static string TryReadGeminiErrorMessage(string responseJson)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(responseJson);
            return jsonDoc.RootElement.TryGetProperty("error", out var error) &&
                   error.TryGetProperty("message", out var message)
                ? message.GetString() ?? "Gemini API error."
                : "Gemini API error.";
        }
        catch
        {
            return "Gemini API error.";
        }
    }

    private static bool IsQuotaOrRateLimit(string message)
    {
        var normalized = message.ToLowerInvariant();
        return normalized.Contains("quota") ||
               normalized.Contains("rate limit") ||
               normalized.Contains("resource_exhausted") ||
               normalized.Contains("too many requests");
    }
}
