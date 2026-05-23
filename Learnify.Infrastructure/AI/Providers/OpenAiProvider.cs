using Learnify.Core.Interfaces;
using Learnify.Core.Models;
using Learnify.Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Learnify.Infrastructure.AI.Providers;

/// <summary>
/// OpenAI API provider implementation.
/// </summary>
public class OpenAiProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly AiSettings.OpenAiSettings _settings;
    private readonly ILogger<OpenAiProvider> _logger;

    public string ProviderName => "OpenAI";

    public OpenAiProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<AiSettings> aiSettings,
        ILogger<OpenAiProvider> logger)
    {
        _httpClient = httpClientFactory.CreateClient("OpenAIClient");
        _settings = aiSettings.Value.OpenAi;
        _logger = logger;
    }

    public async Task<string> CompleteAsync(string prompt, AiRequestOptions options, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new InvalidOperationException(
                "OpenAI API key is not configured. Please set AiSettings:OpenAi:ApiKey in appsettings.json or user secrets.");
        }

        _logger.LogDebug("OpenAI provider: Processing prompt for model {Model}", _settings.Model);

        var fullPrompt = $"You are an educational AI assistant for LearnifyAI, an enterprise educational platform. " +
            $"Provide clear, accurate, and helpful responses tailored for students and educators.\n\n" +
            $"User prompt:\n{prompt}";

        var requestBody = new
        {
            model = _settings.Model,
            messages = new[]
            {
                new { role = "user", content = fullPrompt }
            },
            temperature = options.Temperature,
            max_tokens = options.MaxTokens,
        };

        var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/v1/chat/completions", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(errorBody, "OpenAI API returned error status {StatusCode}", response.StatusCode);
            throw new InvalidOperationException($"OpenAI API error: {response.StatusCode} - {errorBody}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        var document = JsonDocument.Parse(responseJson);

        try
        {
            var text = document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return text?.Trim() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse OpenAI response");
            throw new InvalidOperationException("Failed to parse AI response from OpenAI provider.", ex);
        }
    }
}
