using Learnify.Core.Interfaces;
using Learnify.Core.Models;
using Learnify.Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Learnify.Infrastructure.AI.Providers;

/// <summary>
/// Anthropic Claude AI provider implementation.
/// Uses the Claude API for advanced reasoning and educational content generation.
/// </summary>
public class ClaudeAiProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly AiSettings.ClaudeSettings _settings;
    private readonly ILogger<ClaudeAiProvider> _logger;

    public string ProviderName => "Claude";

    public ClaudeAiProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<AiSettings> aiSettings,
        ILogger<ClaudeAiProvider> logger)
    {
        _httpClient = httpClientFactory.CreateClient("ClaudeClient");
        _settings = aiSettings.Value.Claude;
        _logger = logger;
    }

    public async Task<string> CompleteAsync(string prompt, AiRequestOptions options, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new InvalidOperationException(
                "Claude API key is not configured. Please set AiSettings:Claude:ApiKey in appsettings.json or user secrets.");
        }

        _logger.LogDebug("Claude provider: Processing prompt for model {Model}", _settings.Model);

        var fullPrompt = $"You are an educational AI assistant for LearnifyAI, an enterprise educational platform. " +
            $"Provide clear, accurate, and helpful responses tailored for students and educators.\n\n" +
            $"User prompt:\n{prompt}";

        var requestBody = new
        {
            model = _settings.Model,
            max_tokens = options.MaxTokens,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = fullPrompt
                }
            }
        };

        var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/v1/messages", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(errorBody, "Claude API returned error status {StatusCode}", response.StatusCode);
            throw new InvalidOperationException($"Claude API error: {response.StatusCode} - {errorBody}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        var document = JsonDocument.Parse(responseJson);

        try
        {
            var text = document.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString();
            return text?.Trim() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Claude response");
            throw new InvalidOperationException("Failed to parse AI response from Claude provider.", ex);
        }
    }
}
