using Learnify.Application.Settings;
using Learnify.Core.Interfaces;
using Learnify.Core.Models;
using Learnify.Infrastructure.AI.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Learnify.Infrastructure.AI;

/// <summary>
/// Factory that resolves the configured AI provider at runtime per user.
/// Supports pluggable providers: Gemini, OpenAI, Ollama, Claude, Mock.
/// Implements IAiService as a unified facade.
/// Uses UserAiSettingsStore for per-user provider resolution with rate limiting on default key.
/// </summary>
public class AiProviderFactory : IAiService
{
    private readonly UserAiSettingsStore _store;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AiSettings _aiSettings;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<AiProviderFactory> _logger;

    public AiProviderFactory(
        UserAiSettingsStore store,
        IUnitOfWork unitOfWork,
        IOptions<AiSettings> aiSettings,
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        ILogger<AiProviderFactory> logger)
    {
        _store = store;
        _unitOfWork = unitOfWork;
        _aiSettings = aiSettings.Value;
        _httpContextAccessor = httpContextAccessor;
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?
            .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? _httpContextAccessor.HttpContext?.User?
            .FindFirst("sub")?.Value;

        if (Guid.TryParse(userIdClaim, out var userId))
            return userId;

        throw new UnauthorizedAccessException("User identity not found in token.");
    }

    private async Task<IAiProvider> GetActiveProviderAsync(Guid userId)
    {
        var userSettings = await _unitOfWork.UserAiSettings.GetByUserIdAsync(userId);
        var (storeConfig, isStoreDefault) = _store.Resolve(userId);
        var config = userSettings == null
            ? storeConfig
            : BuildPersistentConfig(userSettings);
        var isDefault = userSettings == null && isStoreDefault;

        if (isDefault && !_store.TryConsumeDefaultKeyRequest(userId))
            throw new InvalidOperationException(
                "Rate limit exceeded (20 requests/hour on default key). " +
                "Use your own API key in AI Settings for unlimited access.");

        _logger.LogInformation("AI provider resolved to '{Provider}' for user {UserId}", config.Provider, userId);

        // If provider needs a key but none is configured, fall back to Mock
        var providerName = config.Provider.ToLowerInvariant();
        var needsKey = providerName is "gemini" or "openai" or "claude";
        if (needsKey && string.IsNullOrWhiteSpace(config.ApiKey))
        {
            _logger.LogWarning(
                "Provider '{Provider}' has no API key. Falling back to MockAiProvider.",
                config.Provider);
            return new MockAiProvider();
        }

        return config.Provider.ToLowerInvariant() switch
        {
            "gemini" => BuildGemini(config),
            "openai" => BuildOpenAi(config),
            "claude" => BuildClaude(config),
            "ollama" => BuildOllama(config),
            "mock"   => new MockAiProvider(),
            _        => new MockAiProvider()
        };
    }

    private UserAiSettingsStore.ProviderConfig BuildPersistentConfig(Learnify.Core.Entities.UserAiSettings settings)
    {
        var provider = NormalizeProvider(settings.ActiveProvider);
        var model = string.IsNullOrWhiteSpace(settings.CustomModel)
            ? GetDefaultModel(provider)
            : settings.CustomModel.Trim();
        var apiKey = provider.ToLowerInvariant() switch
        {
            "gemini" => !string.IsNullOrWhiteSpace(settings.ApiKey) ? settings.ApiKey : _aiSettings.Gemini.ApiKey,
            "openai" => !string.IsNullOrWhiteSpace(settings.ApiKey) ? settings.ApiKey : _aiSettings.OpenAi.ApiKey,
            "claude" => !string.IsNullOrWhiteSpace(settings.ApiKey) ? settings.ApiKey : _aiSettings.Claude.ApiKey,
            _ => string.Empty
        };
        var baseUrl = provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase)
            ? settings.OllamaBaseUrl ?? _aiSettings.Ollama.BaseUrl
            : string.Empty;

        return new UserAiSettingsStore.ProviderConfig(provider, apiKey ?? string.Empty, model, baseUrl);
    }

    private string GetDefaultModel(string provider) =>
        provider.ToLowerInvariant() switch
        {
            "openai" => _aiSettings.OpenAi.Model,
            "claude" => _aiSettings.Claude.Model,
            "ollama" => _aiSettings.Ollama.Model,
            _ => _aiSettings.Gemini.Model
        };

    private static string NormalizeProvider(string provider) =>
        provider.ToLowerInvariant() switch
        {
            "openai" => "OpenAI",
            "claude" => "Claude",
            "ollama" => "Ollama",
            _ => "Gemini"
        };

    private GeminiAiProvider BuildGemini(UserAiSettingsStore.ProviderConfig config)
    {
        var geminiSettings = new AiSettings.GeminiSettings
        {
            ApiKey = config.ApiKey,
            Model = config.Model
        };
        var optionsWrapper = new OptionsWrapper<AiSettings>(new AiSettings
        {
            Gemini = geminiSettings
        });
        return new GeminiAiProvider(
            _httpClientFactory,
            optionsWrapper,
            _loggerFactory.CreateLogger<GeminiAiProvider>());
    }

    private OpenAiProvider BuildOpenAi(UserAiSettingsStore.ProviderConfig config)
    {
        var openAiSettings = new AiSettings.OpenAiSettings
        {
            ApiKey = config.ApiKey,
            Model = config.Model
        };
        var optionsWrapper = new OptionsWrapper<AiSettings>(new AiSettings
        {
            OpenAi = openAiSettings
        });
        return new OpenAiProvider(
            _httpClientFactory,
            optionsWrapper,
            _loggerFactory.CreateLogger<OpenAiProvider>());
    }

    private ClaudeAiProvider BuildClaude(UserAiSettingsStore.ProviderConfig config)
    {
        var claudeSettings = new AiSettings.ClaudeSettings
        {
            ApiKey = config.ApiKey,
            Model = config.Model
        };
        var optionsWrapper = new OptionsWrapper<AiSettings>(new AiSettings
        {
            Claude = claudeSettings
        });
        return new ClaudeAiProvider(
            _httpClientFactory,
            optionsWrapper,
            _loggerFactory.CreateLogger<ClaudeAiProvider>());
    }

    private OllamaAiProvider BuildOllama(UserAiSettingsStore.ProviderConfig config)
    {
        var baseUrl = string.IsNullOrWhiteSpace(config.BaseUrl)
            ? "http://127.0.0.1:8080"
            : config.BaseUrl.Trim();

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException($"Invalid Ollama BaseUrl configured: {baseUrl}");
        }

        var httpClient = new HttpClient
        {
            BaseAddress = baseUri
        };

        return new OllamaAiProvider(
            httpClient,
            config.Model,
            _loggerFactory.CreateLogger<OllamaAiProvider>());
    }

    public async Task<string> SummarizeNoteAsync(string content, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var provider = await GetActiveProviderAsync(userId);
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
        var userId = GetCurrentUserId();
        var provider = await GetActiveProviderAsync(userId);
        _logger.LogDebug("Generating {Count} flashcards via '{Provider}'", count, provider.ProviderName);
        var prompt = $"Generate exactly {count} flashcard pairs from the following study material. " +
                     $"Each flashcard should have a 'question' and 'answer'. " +
                     $"Return the result as a JSON array of objects with 'question' and 'answer' properties. " +
                     $"Do NOT include any markdown formatting or code blocks - return ONLY the raw JSON array.\n\n" +
                     $"Material:\n{content}";

        var options = new AiRequestOptions { MaxTokens = 2000, Temperature = 0.7f };
        var response = await provider.CompleteAsync(prompt, options, ct);
        return ParseFlashcards(response);
    }

    public async Task<string> GetStudyTipsAsync(string topic, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var provider = await GetActiveProviderAsync(userId);
        _logger.LogDebug("Generating study tips for '{Topic}' via '{Provider}'", topic, provider.ProviderName);
        var prompt = $"Provide 3-5 personalized study tips for a student learning about: {topic}. " +
                     $"Focus on active recall, spaced repetition, and concept mastery techniques.";

        var options = new AiRequestOptions { MaxTokens = 500, Temperature = 0.5f };
        var response = await provider.CompleteAsync(prompt, options, ct);
        return response;
    }

    public async Task<NoteAnalysisResult> AnalyzeDocumentAsync(
        string content,
        string fileName,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var provider = await GetActiveProviderAsync(userId);

        var snippet = content[..Math.Min(content.Length, 8000)];
        var prompt = $"You are analyzing an educational document for a student note-taking app. " +
                     $"The document is named '{fileName}'. Analyze the following content and return ONLY a " +
                     $"raw JSON object (no markdown, no code blocks) with these exact fields:\n" +
                     $"- suggestedCourseName: a short course name this document belongs to (e.g. 'Data Structures', 'Web Development')\n" +
                     $"- summary: 2-3 sentence summary of the document\n" +
                     $"- detectedTopics: a JSON array of strings, each being a chapter or topic heading found\n\n" +
                     $"Document content:\n{snippet}";

        var options = new AiRequestOptions { MaxTokens = 1000, Temperature = 0.2f };
        var response = await provider.CompleteAsync(prompt, options, ct);

        return ParseAnalysisResult(response, content);
    }

    private static List<FlashcardResult> ParseFlashcards(string response)
    {
        var clean = response.Trim().TrimStart('`').TrimEnd('`');
        if (clean.StartsWith("json", StringComparison.OrdinalIgnoreCase))
        {
            clean = clean[4..].Trim();
        }

        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        try
        {
            var flashcards = System.Text.Json.JsonSerializer.Deserialize<List<FlashcardJsonItem>>(clean, options);
            return flashcards?
                .Select(item => new FlashcardResult(
                    item.Question ?? item.Term ?? string.Empty,
                    item.Answer ?? item.Definition ?? string.Empty))
                .Where(item =>
                    !string.IsNullOrWhiteSpace(item.Question) &&
                    !string.IsNullOrWhiteSpace(item.Answer))
                .ToList() ?? new List<FlashcardResult>();
        }
        catch
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<FlashcardResult>>(clean, options)
                    ?? new List<FlashcardResult>();
            }
            catch
            {
                return new List<FlashcardResult>();
            }
        }
    }

    private sealed class FlashcardJsonItem
    {
        public string? Question { get; set; }
        public string? Answer { get; set; }
        public string? Term { get; set; }
        public string? Definition { get; set; }
    }

    private static NoteAnalysisResult ParseAnalysisResult(string response, string originalContent)
    {
        try
        {
            var clean = response.Trim().TrimStart('`').TrimEnd('`');
            if (clean.StartsWith("json", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean[4..].Trim();
            }

            using var document = System.Text.Json.JsonDocument.Parse(clean);
            var root = document.RootElement;

            return new NoteAnalysisResult
            {
                SuggestedCourseName = root.TryGetProperty("suggestedCourseName", out var courseName)
                    ? courseName.GetString() ?? "General Notes"
                    : "General Notes",
                Summary = root.TryGetProperty("summary", out var summary)
                    ? summary.GetString() ?? string.Empty
                    : string.Empty,
                DetectedTopics = root.TryGetProperty("detectedTopics", out var topics)
                    ? topics.EnumerateArray()
                        .Select(topic => topic.GetString() ?? string.Empty)
                        .Where(topic => !string.IsNullOrWhiteSpace(topic))
                        .ToList()
                    : new List<string>(),
                ExtractedText = originalContent
            };
        }
        catch
        {
            return new NoteAnalysisResult
            {
                SuggestedCourseName = "General Notes",
                Summary = response,
                DetectedTopics = new List<string>(),
                ExtractedText = originalContent
            };
        }
    }
}
