using Learnify.Application.Settings;
using Learnify.Core.Interfaces;
using Learnify.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Learnify.Infrastructure.AI.Providers;

/// <summary>
/// OpenAI-compatible local AI provider for llama.cpp and similar local servers.
/// </summary>
public class LocalOpenAiProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly AiSettings.LocalOpenAiSettings _settings;
    private readonly ILogger<LocalOpenAiProvider> _logger;

    public string ProviderName => "LocalOpenAI";

    public LocalOpenAiProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<AiSettings> aiSettings,
        ILogger<LocalOpenAiProvider> logger)
    {
        _httpClient = httpClientFactory.CreateClient("LocalOpenAIClient");
        _settings = aiSettings.Value.LocalOpenAI;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        }
    }

    public LocalOpenAiProvider(
        HttpClient httpClient,
        string model,
        string apiKey,
        ILogger<LocalOpenAiProvider> logger)
    {
        _httpClient = httpClient;
        _settings = new AiSettings.LocalOpenAiSettings
        {
            BaseUrl = httpClient.BaseAddress?.ToString() ?? string.Empty,
            Model = model,
            ApiKey = apiKey
        };
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);
        }
    }

    public async Task<string> CompleteAsync(string prompt, AiRequestOptions options, CancellationToken ct = default)
    {
        _logger.LogDebug("LocalOpenAI provider: Processing prompt for model {Model}", _settings.Model);

        var requestBody = new
        {
            model = _settings.Model,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = "You are an educational AI assistant for LearnifyAI. Return concise, accurate responses and follow output format instructions exactly."
                },
                new { role = "user", content = prompt }
            },
            temperature = options.Temperature,
            max_tokens = options.MaxTokens,
            stream = false
        };

        var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/v1/chat/completions", content, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "LocalOpenAI API returned error status {StatusCode}: {ErrorBody}",
                response.StatusCode,
                errorBody);
            throw new InvalidOperationException($"LocalOpenAI API error: {response.StatusCode} - {errorBody}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        using var document = JsonDocument.Parse(responseJson);

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
            _logger.LogError(ex, "Failed to parse LocalOpenAI response");
            throw new InvalidOperationException("Failed to parse AI response from LocalOpenAI provider.", ex);
        }
    }
}
