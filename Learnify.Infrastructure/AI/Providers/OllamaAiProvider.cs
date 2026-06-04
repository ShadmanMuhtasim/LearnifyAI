using Learnify.Core.Interfaces;
using Learnify.Core.Models;
using Learnify.Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Learnify.Infrastructure.AI.Providers;

/// <summary>
/// Ollama local AI provider implementation.
/// Runs on a local Ollama instance for privacy-first AI features.
/// </summary>
public class OllamaAiProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly AiSettings.OllamaSettings _settings;
    private readonly ILogger<OllamaAiProvider> _logger;

    public string ProviderName => "Ollama";

    public OllamaAiProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<AiSettings> aiSettings,
        ILogger<OllamaAiProvider> logger)
    {
        _httpClient = httpClientFactory.CreateClient("OllamaClient");
        _settings = aiSettings.Value.Ollama;
        _logger = logger;
    }

    /// <summary>
    /// Used by AiProviderFactory for runtime BaseUrl configuration.
    /// </summary>
    public OllamaAiProvider(HttpClient httpClient, string model, ILogger<OllamaAiProvider> logger)
    {
        _httpClient = httpClient;
        _settings = new AiSettings.OllamaSettings
        {
            BaseUrl = httpClient.BaseAddress?.ToString() ?? string.Empty,
            Model = model
        };
        _logger = logger;
    }

    public async Task<string> CompleteAsync(string prompt, AiRequestOptions options, CancellationToken ct = default)
    {
        _logger.LogDebug("Ollama provider: Processing prompt for model {Model}", _settings.Model);

        var fullPrompt = $"You are an educational AI assistant for LearnifyAI, an enterprise educational platform. " +
            $"Provide clear, accurate, and helpful responses tailored for students and educators.\n\n" +
            $"User prompt:\n{prompt}";

        var requestBody = new
        {
            model = _settings.Model,
            prompt = fullPrompt,
            options = new
            {
                temperature = options.Temperature,
                num_predict = options.MaxTokens,
            },
            stream = false
        };

        var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/generate", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Ollama API returned error status {StatusCode}: {ErrorBody}",
                response.StatusCode,
                errorBody);
            throw new InvalidOperationException($"Ollama API error: {response.StatusCode} - {errorBody}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        var document = JsonDocument.Parse(responseJson);

        try
        {
            var text = document.RootElement.GetProperty("response").GetString();
            return text?.Trim() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Ollama response");
            throw new InvalidOperationException("Failed to parse AI response from Ollama provider.", ex);
        }
    }
}
