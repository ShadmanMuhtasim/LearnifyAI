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
            throw new InvalidOperationException(
                "Gemini API key is not configured. Please set AiSettings:Gemini:ApiKey in appsettings.json or user secrets.");
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
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct);

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
                _logger.LogError("Gemini API error: {ErrorMessage}", errorMessage.GetString());
                throw new InvalidOperationException(
                    $"Gemini API error: {errorMessage.GetString()}");
            }

            _logger.LogWarning("Could not parse Gemini response. Raw: {Response}", responseJson);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Gemini response");
            throw;
        }
    }
}