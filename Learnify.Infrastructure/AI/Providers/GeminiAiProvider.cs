using Learnify.Application.Settings;
using Learnify.Core.Interfaces;
using Learnify.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
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
                ProviderName,
                "Gemini API key is not configured. Add it in user secrets or Settings.",
                "",
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
            var errorPreview = ExtractProviderErrorPreview(responseJson);
            _logger.LogWarning(
                "Gemini provider request failed. Provider={Provider}, Model={Model}, StatusCode={StatusCode}, ResponseLength={ResponseLength}, ErrorPreview={ErrorPreview}, HasApiKey={HasApiKey}",
                ProviderName,
                _geminiSettings.Model,
                (int)response.StatusCode,
                responseJson.Length,
                errorPreview,
                !string.IsNullOrWhiteSpace(_geminiSettings.ApiKey));

            throw BuildHttpError(response.StatusCode, errorPreview);
        }

        try
        {
            using var jsonDoc = JsonDocument.Parse(responseJson);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("candidates", out var candidates)
                && candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                if (firstCandidate.TryGetProperty("content", out var content)
                    && content.TryGetProperty("parts", out var parts)
                    && parts.ValueKind == JsonValueKind.Array
                    && parts.GetArrayLength() > 0
                    && parts[0].TryGetProperty("text", out var textElement))
                {
                    var text = textElement.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        _logger.LogDebug("Gemini response received for model {Model}", _geminiSettings.Model);
                        return text.Trim();
                    }
                }

                var finishReason = firstCandidate.TryGetProperty("finishReason", out var finishReasonElement)
                    ? finishReasonElement.GetString()
                    : null;
                _logger.LogWarning(
                    "Gemini response did not include non-empty text. Provider={Provider}, Model={Model}, FinishReason={FinishReason}, ResponseLength={ResponseLength}",
                    ProviderName,
                    _geminiSettings.Model,
                    finishReason ?? "",
                    responseJson.Length);

                throw new AiProviderException(
                    "AI_PROVIDER_UNAVAILABLE",
                    ProviderName,
                    string.IsNullOrWhiteSpace(finishReason)
                        ? "Gemini provider returned an empty response."
                        : $"Gemini provider returned no text (finishReason={finishReason}).",
                    finishReason ?? "",
                    502);
            }

            // Try to get an error message from the response
            if (root.TryGetProperty("error", out var error)
                && error.TryGetProperty("message", out var errorMessage))
            {
                var preview = SanitizePreview(errorMessage.GetString() ?? "Unknown Gemini API error.");
                _logger.LogWarning(
                    "Gemini provider returned error payload. Provider={Provider}, Model={Model}, ResponseLength={ResponseLength}, ErrorPreview={ErrorPreview}",
                    ProviderName,
                    _geminiSettings.Model,
                    responseJson.Length,
                    preview);
                throw IsQuotaOrRateLimit(preview)
                    ? new AiProviderException(
                        "AI_RATE_LIMIT",
                        ProviderName,
                        "Gemini quota or rate limit was reached. Try again later, switch provider, or use a local/free mode if available.",
                        preview,
                        429)
                    : new AiProviderException(
                        "AI_PROVIDER_UNAVAILABLE",
                        ProviderName,
                        $"Gemini provider error: {preview}",
                        preview,
                        502);
            }

            _logger.LogWarning(
                "Could not parse Gemini response shape. Provider={Provider}, Model={Model}, ResponseLength={ResponseLength}",
                ProviderName,
                _geminiSettings.Model,
                responseJson.Length);
            throw new AiProviderException(
                "AI_PROVIDER_UNAVAILABLE",
                ProviderName,
                "Gemini provider returned an unrecognized response shape.",
                "",
                502);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to parse Gemini response JSON. Provider={Provider}, Model={Model}, ResponseLength={ResponseLength}",
                ProviderName,
                _geminiSettings.Model,
                responseJson.Length);
            throw new AiProviderException(
                "AI_PROVIDER_UNAVAILABLE",
                ProviderName,
                "Gemini provider returned invalid JSON.",
                "",
                502,
                ex);
        }
    }

    private static AiProviderException BuildHttpError(HttpStatusCode statusCode, string errorPreview)
    {
        if ((int)statusCode == 429 || IsQuotaOrRateLimit(errorPreview))
        {
            return new AiProviderException(
                "AI_RATE_LIMIT",
                "Gemini",
                "Gemini quota or rate limit was reached. Try again later, switch provider, or use a local/free mode if available.",
                errorPreview,
                429);
        }

        if (statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return new AiProviderException(
                "AI_PROVIDER_UNAVAILABLE",
                "Gemini",
                "Gemini provider authentication failed. Check AI provider settings.",
                errorPreview,
                502);
        }

        if (statusCode == HttpStatusCode.NotFound)
        {
            return new AiProviderException(
                "AI_PROVIDER_UNAVAILABLE",
                "Gemini",
                $"Gemini provider model or endpoint was not found. Check AI provider settings. {errorPreview}",
                errorPreview,
                502);
        }

        return new AiProviderException(
            "AI_PROVIDER_UNAVAILABLE",
            "Gemini",
            $"Gemini provider error ({(int)statusCode}). {errorPreview}",
            errorPreview,
            502);
    }

    private static string ExtractProviderErrorPreview(string responseJson)
    {
        try
        {
            using var document = JsonDocument.Parse(responseJson);
            var root = document.RootElement;
            if (root.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var message))
            {
                return SanitizePreview(message.GetString() ?? "");
            }
        }
        catch (JsonException)
        {
            // Fall through to raw sanitized preview.
        }

        return SanitizePreview(responseJson);
    }

    private static string SanitizePreview(string value)
    {
        var compact = string.Join(" ", value.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        return compact.Length <= 240 ? compact : compact[..240];
    }

    private static bool IsQuotaOrRateLimit(string value)
        => value.Contains("quota", StringComparison.OrdinalIgnoreCase) ||
           value.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
           value.Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase) ||
           value.Contains("TooManyRequests", StringComparison.OrdinalIgnoreCase) ||
           value.Contains("exceeded your current quota", StringComparison.OrdinalIgnoreCase);
}
