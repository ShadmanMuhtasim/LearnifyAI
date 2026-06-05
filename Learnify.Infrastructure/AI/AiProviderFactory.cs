using Learnify.Application.Settings;
using Learnify.Core.Interfaces;
using Learnify.Core.Models;
using Learnify.Infrastructure.AI.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Learnify.Infrastructure.AI;

/// <summary>
/// Factory that resolves the configured AI provider at runtime per user.
/// Supports pluggable providers: Gemini, OpenAI, Ollama, LocalOpenAI, Claude, Mock.
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
        => (await GetActiveProviderWithConfigAsync(userId)).Provider;

    private async Task<(IAiProvider Provider, UserAiSettingsStore.ProviderConfig Config)> GetActiveProviderWithConfigAsync(Guid userId)
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
            return (new MockAiProvider(), new UserAiSettingsStore.ProviderConfig("Mock", string.Empty, "mock", string.Empty));
        }

        IAiProvider provider = config.Provider.ToLowerInvariant() switch
        {
            "gemini" => BuildGemini(config),
            "openai" => BuildOpenAi(config),
            "claude" => BuildClaude(config),
            "ollama" => BuildOllama(config),
            "localopenai" => BuildLocalOpenAi(config),
            "mock"   => new MockAiProvider(),
            _        => new MockAiProvider()
        };

        return (provider, config);
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
            "localopenai" => settings.ApiKey ?? _aiSettings.LocalOpenAI.ApiKey,
            _ => string.Empty
        };
        var baseUrl = provider.ToLowerInvariant() switch
        {
            "ollama" => settings.OllamaBaseUrl ?? _aiSettings.Ollama.BaseUrl,
            "localopenai" => settings.LocalOpenAiBaseUrl ?? _aiSettings.LocalOpenAI.BaseUrl,
            _ => string.Empty
        };

        return new UserAiSettingsStore.ProviderConfig(provider, apiKey ?? string.Empty, model, baseUrl);
    }

    private string GetDefaultModel(string provider) =>
        provider.ToLowerInvariant() switch
        {
            "openai" => _aiSettings.OpenAi.Model,
            "claude" => _aiSettings.Claude.Model,
            "ollama" => _aiSettings.Ollama.Model,
            "localopenai" => _aiSettings.LocalOpenAI.Model,
            _ => _aiSettings.Gemini.Model
        };

    private static string NormalizeProvider(string provider) =>
        provider.ToLowerInvariant() switch
        {
            "openai" => "OpenAI",
            "claude" => "Claude",
            "ollama" => "Ollama",
            "localopenai" => "LocalOpenAI",
            "localopenai-compatible" => "LocalOpenAI",
            "local openai" => "LocalOpenAI",
            "local openai-compatible / llama.cpp" => "LocalOpenAI",
            "llama.cpp" => "LocalOpenAI",
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

    private LocalOpenAiProvider BuildLocalOpenAi(UserAiSettingsStore.ProviderConfig config)
    {
        var baseUrl = string.IsNullOrWhiteSpace(config.BaseUrl)
            ? "http://127.0.0.1:8080"
            : config.BaseUrl.Trim();

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException($"Invalid LocalOpenAI BaseUrl configured: {baseUrl}");
        }

        var httpClient = new HttpClient
        {
            BaseAddress = baseUri
        };

        var model = string.IsNullOrWhiteSpace(config.Model)
            ? _aiSettings.LocalOpenAI.Model
            : config.Model;

        return new LocalOpenAiProvider(
            httpClient,
            model,
            config.ApiKey,
            _loggerFactory.CreateLogger<LocalOpenAiProvider>());
    }

    public async Task<string> SummarizeNoteAsync(string content, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var provider = await GetActiveProviderAsync(userId);
        _logger.LogDebug("Summarizing note via '{Provider}'", provider.ProviderName);
        // Reasoning local models can consume a large part of the completion budget
        // before emitting final chat content. Keep this high enough for LocalOpenAI
        // while still asking every provider for a short summary.
        var options = new AiRequestOptions { MaxTokens = 3000, Temperature = 0.3f };
        var response = await provider.CompleteAsync(
            $"Summarize the following educational content in 2-3 concise sentences. " +
            $"Return only the final summary text, with no reasoning or preamble:\n\n{content}",
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
        var flashcards = ParseFlashcards(response);
        if (flashcards.Count == 0)
        {
            throw new InvalidOperationException("AI flashcard response did not contain any valid cards.");
        }

        return flashcards;
    }

    public async Task<GeneratedQuizResult> GenerateQuizAsync(
        string content,
        IReadOnlyList<string> questionTypes,
        string difficulty,
        int numberOfQuestions,
        CancellationToken ct = default)
    {
        var normalizedTypes = NormalizeQuestionTypes(questionTypes);
        if (normalizedTypes.Count == 0)
        {
            normalizedTypes.Add("MultipleChoice");
        }

        var requestedCount = Math.Clamp(numberOfQuestions, 1, 20);
        var normalizedDifficulty = NormalizeDifficulty(difficulty);
        var userId = GetCurrentUserId();
        var (provider, providerConfig) = await GetActiveProviderWithConfigAsync(userId);
        var isLocalOpenAi = provider.ProviderName.Equals("LocalOpenAI", StringComparison.OrdinalIgnoreCase);

        if (isLocalOpenAi && requestedCount > 5)
        {
            return await LocalOpenAiQuizBatchGenerator.GenerateAsync(
                provider,
                providerConfig.Model,
                content,
                normalizedTypes,
                normalizedDifficulty,
                requestedCount,
                _logger,
                ct);
        }

        var prompt = isLocalOpenAi
            ? BuildLocalOpenAiQuizPrompt(content, normalizedTypes, normalizedDifficulty, requestedCount)
            : BuildStandardQuizPrompt(content, normalizedTypes, normalizedDifficulty, requestedCount);

        var options = new AiRequestOptions { MaxTokens = isLocalOpenAi ? 2600 : 3500, Temperature = 0.1f };
        var response = await provider.CompleteAsync(prompt, options, ct);
        try
        {
            return ParseQuiz(response);
        }
        catch (AiQuizFormatException ex)
        {
            _logger.LogWarning(
                ex,
                "AI quiz parse failed. Provider={Provider}, Model={Model}, ResponseLength={ResponseLength}, Reason={Reason}, Preview={Preview}",
                provider.ProviderName,
                providerConfig.Model,
                ex.ResponseLength,
                ex.Reason,
                ex.OutputPreview);
            throw;
        }
    }

    private static string BuildStandardQuizPrompt(
        string content,
        IReadOnlyList<string> normalizedTypes,
        string normalizedDifficulty,
        int requestedCount) =>
        "Create a quiz from the study material.\n" +
        "Return ONLY valid JSON. No markdown. No code fences. No explanation. No commentary. No XML tags. No <think> tags. No trailing text.\n" +
        "Use double quotes for all JSON strings and property names.\n" +
        "Schema:\n" +
        "{\"title\":\"string\",\"questions\":[{\"type\":\"MultipleChoice\",\"questionText\":\"string\",\"options\":[\"option 1\",\"option 2\",\"option 3\",\"option 4\"],\"correctAnswer\":\"option 1\",\"explanation\":\"string\"}]}\n" +
        $"Rules: exactly {requestedCount} questions; difficulty {normalizedDifficulty}; allowed types: {string.Join(", ", normalizedTypes)}.\n" +
        "MultipleChoice: include 4 concise options; correctAnswer must exactly match one option.\n" +
        "TrueFalse: options must be [\"True\",\"False\"]; correctAnswer must be \"True\" or \"False\".\n" +
        "ShortAnswer: options must be []; correctAnswer must be concise.\n" +
        "FillInTheBlank: questionText must contain ____; options must be []; correctAnswer must be concise.\n" +
        "Study material:\n" +
        content[..Math.Min(content.Length, 9000)];

    private static string BuildLocalOpenAiQuizPrompt(
        string content,
        IReadOnlyList<string> normalizedTypes,
        string normalizedDifficulty,
        int requestedCount) =>
        "/no_think\n" +
        "Return final JSON only. Do not include reasoning, markdown, commentary, XML tags, or code fences. No trailing text.\n" +
        "Schema: {\"title\":\"string\",\"questions\":[{\"type\":\"MultipleChoice\",\"questionText\":\"string\",\"options\":[\"A\",\"B\",\"C\",\"D\"],\"correctAnswer\":\"A\",\"explanation\":\"string\"}]}\n" +
        $"Generate exactly {requestedCount} questions. Difficulty: {normalizedDifficulty}. Allowed types: {string.Join(", ", normalizedTypes)}.\n" +
        "MultipleChoice: 4 options; correctAnswer exactly matches one option. TrueFalse: options [\"True\",\"False\"]. ShortAnswer and FillInTheBlank: options []. FillInTheBlank uses ____.\n" +
        "Study material:\n" +
        content[..Math.Min(content.Length, 6500)];

    public async Task<string> GetStudyTipsAsync(string topic, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new InvalidOperationException("Study tips require note content or a topic.");
        }

        var userId = GetCurrentUserId();
        var provider = await GetActiveProviderAsync(userId);
        _logger.LogDebug("Generating study tips via '{Provider}'", provider.ProviderName);
        var snippet = topic[..Math.Min(topic.Length, 8000)];
        var prompt = "Create concise study guidance based only on this note content. " +
                     "Return markdown/plain text with line breaks preserved and exactly these sections:\n" +
                     "## Study Tips\n" +
                     "1. Active Recall Questions\n" +
                     "2. Key Concepts to Master\n" +
                     "3. Common Confusions\n" +
                     "4. Memory Hooks / Analogies\n" +
                     "5. Mini Study Plan\n" +
                     "6. Self-Test Questions\n\n" +
                     "Under each numbered section, include 2-4 content-specific bullets. " +
                     "Do not invent topics that are not supported by the note. Return only the final study tips.\n\n" +
                     $"Note content:\n{snippet}";

        var options = new AiRequestOptions { MaxTokens = 2500, Temperature = 0.35f };
        var response = await provider.CompleteAsync(prompt, options, ct);
        if (string.IsNullOrWhiteSpace(response))
        {
            throw new InvalidOperationException("AI provider returned an empty study tips response.");
        }

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
                     $"The document is named '{fileName}'. Analyze the content and return ONLY one compact " +
                     $"raw JSON object. Do not include markdown, code fences, reasoning, prose, or comments. " +
                     $"Use these exact fields:\n" +
                     $"- suggestedCourseName: a short course name this document belongs to (e.g. 'Data Structures', 'Web Development')\n" +
                     $"- summary: 2-3 sentence summary of the document\n" +
                     $"- detectedTopics: a JSON array of strings, each being a chapter or topic heading found\n\n" +
                     $"Document content:\n{snippet}";

        var options = new AiRequestOptions { MaxTokens = 2000, Temperature = 0.2f };
        var response = await provider.CompleteAsync(prompt, options, ct);
        if (string.IsNullOrWhiteSpace(response))
        {
            throw new InvalidOperationException("AI provider returned an empty document analysis response.");
        }

        return ParseAnalysisResult(response, content);
    }

    private static List<FlashcardResult> ParseFlashcards(string response)
    {
        var clean = CleanJsonResponse(response);

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

    private static GeneratedQuizResult ParseQuiz(string response)
        => QuizResponseParser.Parse(response);

    private static List<string> ReadOptions(JsonElement question)
    {
        if (!question.TryGetProperty("options", out var optionsElement) ||
            optionsElement.ValueKind != JsonValueKind.Array)
        {
            return new List<string>();
        }

        return optionsElement.EnumerateArray()
            .Select(option => option.GetString()?.Trim() ?? string.Empty)
            .Where(option => !string.IsNullOrWhiteSpace(option))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string CleanJsonResponse(string response)
    {
        var clean = response.Trim();
        if (clean.StartsWith("```", StringComparison.Ordinal))
        {
            clean = clean[3..].Trim();
            if (clean.StartsWith("json", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean[4..].Trim();
            }

            var fenceIndex = clean.LastIndexOf("```", StringComparison.Ordinal);
            if (fenceIndex >= 0)
            {
                clean = clean[..fenceIndex].Trim();
            }
        }

        clean = clean.Trim().TrimStart('`').TrimEnd('`').Trim();
        var objectStart = clean.IndexOf('{');
        var arrayStart = clean.IndexOf('[');
        var start = objectStart >= 0 && (arrayStart < 0 || objectStart < arrayStart)
            ? objectStart
            : arrayStart;

        if (start >= 0)
        {
            var end = clean[start] == '{'
                ? clean.LastIndexOf('}')
                : clean.LastIndexOf(']');

            if (end > start)
            {
                clean = clean[start..(end + 1)];
            }
        }

        return clean.Trim();
    }

    private static List<string> NormalizeQuestionTypes(IReadOnlyList<string> questionTypes)
    {
        return questionTypes
            .Select(NormalizeQuestionType)
            .Where(type => type is "MultipleChoice" or "TrueFalse" or "ShortAnswer" or "FillInTheBlank")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeQuestionType(string type)
    {
        var normalized = type.Replace(" ", "", StringComparison.OrdinalIgnoreCase)
            .Replace("-", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        return normalized.ToLowerInvariant() switch
        {
            "truefalse" => "TrueFalse",
            "shortanswer" => "ShortAnswer",
            "fillintheblank" => "FillInTheBlank",
            "fillblank" => "FillInTheBlank",
            _ => "MultipleChoice"
        };
    }

    private static string NormalizeDifficulty(string difficulty) =>
        difficulty.Trim().ToLowerInvariant() switch
        {
            "easy" => "Easy",
            "hard" => "Hard",
            _ => "Medium"
        };

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
            var clean = CleanJsonResponse(response);

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
