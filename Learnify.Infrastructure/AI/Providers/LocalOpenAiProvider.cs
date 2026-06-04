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

        var endpointPath = "/v1/chat/completions";
        var responseJson = await response.Content.ReadAsStringAsync(ct);
        using var document = JsonDocument.Parse(responseJson);

        try
        {
            var choice = document.RootElement.GetProperty("choices")[0];
            var message = choice.GetProperty("message");
            var text = message.GetProperty("content").GetString();

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning(
                    "LocalOpenAI response content was empty. Provider={Provider}, Endpoint={Endpoint}, Model={Model}, StatusCode={StatusCode}, Shape={ResponseShape}",
                    ProviderName,
                    endpointPath,
                    _settings.Model,
                    response.StatusCode,
                    DescribeResponseShape(document.RootElement));

                throw new InvalidOperationException(
                    "LocalOpenAI response did not include non-empty choices[0].message.content.");
            }

            return text.Trim();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("choices[0].message.content", StringComparison.Ordinal))
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse LocalOpenAI response");
            throw new InvalidOperationException("Failed to parse AI response from LocalOpenAI provider.", ex);
        }
    }

    private static string DescribeResponseShape(JsonElement root)
    {
        var choicesCount = root.TryGetProperty("choices", out var choices) &&
                           choices.ValueKind == JsonValueKind.Array
            ? choices.GetArrayLength()
            : 0;
        var firstChoice = choicesCount > 0 ? choices[0] : default;
        var finishReason = firstChoice.ValueKind == JsonValueKind.Object &&
                           firstChoice.TryGetProperty("finish_reason", out var finishReasonElement)
            ? finishReasonElement.GetString() ?? ""
            : "";
        var hasMessage = false;
        var hasContent = false;
        var hasReasoningContent = false;
        if (firstChoice.ValueKind == JsonValueKind.Object &&
            firstChoice.TryGetProperty("message", out var message) &&
            message.ValueKind == JsonValueKind.Object)
        {
            hasMessage = true;
            hasContent = message.TryGetProperty("content", out _);
            hasReasoningContent = message.TryGetProperty("reasoning_content", out _);
        }

        return $"choices={choicesCount};finish_reason={finishReason};message={hasMessage};content={hasContent};reasoning_content={hasReasoningContent}";
    }
}
