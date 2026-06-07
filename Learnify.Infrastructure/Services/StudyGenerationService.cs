using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Learnify.Application.Settings;
using Learnify.Core.Entities;
using Learnify.Core.Interfaces;
using Learnify.Core.Models;
using Learnify.Infrastructure.AI;
using Learnify.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Learnify.Infrastructure.Services;

public sealed class StudyGenerationService : IStudyGenerationService
{
    private const string FallbackNotice = "AI provider was unavailable or rate-limited, so Learnify used free local generation.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAiService _aiService;
    private readonly ILocalStudyToolService _localStudyToolService;
    private readonly ApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserAiSettingsStore _store;
    private readonly AiSettings _aiSettings;
    private readonly ILogger<StudyGenerationService> _logger;

    public StudyGenerationService(
        IAiService aiService,
        ILocalStudyToolService localStudyToolService,
        ApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        UserAiSettingsStore store,
        IOptions<AiSettings> aiSettings,
        ILogger<StudyGenerationService> logger)
    {
        _aiService = aiService;
        _localStudyToolService = localStudyToolService;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _store = store;
        _aiSettings = aiSettings.Value;
        _logger = logger;
    }

    public async Task<StudyGenerationResult<string>> GenerateSummaryAsync(
        Guid userId,
        string content,
        string generationMode,
        string summaryDepth,
        bool useCache,
        bool regenerate,
        CancellationToken cancellationToken = default)
    {
        var mode = NormalizeMode(generationMode);
        var depth = NormalizeDepth(summaryDepth);
        var provider = await ResolveProviderAsync(userId);
        var sourceHash = Hash(content, "Summary", provider.Provider, provider.Model, mode, depth);

        var cached = await TryGetMarkdownCacheAsync<string>(
            userId, "Summary", sourceHash, provider.Provider, provider.Model, mode, depth, useCache, regenerate, cancellationToken);
        if (cached is not null)
        {
            return cached with { FromCache = true };
        }

        if (mode == "FreeLocal")
        {
            var local = _localStudyToolService.GenerateSummary(content, depth);
            await SaveMarkdownCacheAsync(userId, "Summary", sourceHash, provider.Provider, provider.Model, mode, depth, local, null, cancellationToken);
            return new StudyGenerationResult<string>(local, "FreeLocal", null, false, "Generated locally without AI. Quality may be simpler than AI-generated output.");
        }

        try
        {
            var aiSummary = await _aiService.SummarizeNoteAsync(BuildSummaryInput(content, depth), cancellationToken);
            await SaveMarkdownCacheAsync(userId, "Summary", sourceHash, provider.Provider, provider.Model, mode, depth, aiSummary, null, cancellationToken);
            return new StudyGenerationResult<string>(aiSummary, "AIProvider", provider.Provider, false, null);
        }
        catch (Exception ex) when (mode == "Auto" && IsFallbackException(ex))
        {
            _logger.LogWarning(ex, "AI summary failed in Auto mode. Falling back to local generation. Provider={Provider}", provider.Provider);
            var local = _localStudyToolService.GenerateSummary(content, depth);
            await SaveMarkdownCacheAsync(userId, "Summary", sourceHash, provider.Provider, provider.Model, mode, depth, local, FallbackNotice, cancellationToken);
            return new StudyGenerationResult<string>(local, "FreeLocal", provider.Provider, false, FallbackNotice, ToErrorCode(ex));
        }
        catch (Exception ex)
        {
            throw ToProviderException(ex, provider.Provider);
        }
    }

    public async Task<StudyGenerationResult<IReadOnlyList<FlashcardResult>>> GenerateFlashcardsAsync(
        Guid userId,
        string content,
        int count,
        string generationMode,
        bool useCache,
        bool regenerate,
        CancellationToken cancellationToken = default)
    {
        var mode = NormalizeMode(generationMode);
        var provider = await ResolveProviderAsync(userId);
        var cardCount = Math.Clamp(count, 1, 30);
        var sourceHash = Hash(content, "Flashcards", provider.Provider, provider.Model, mode, cardCount.ToString());

        var cached = await TryGetJsonCacheAsync<IReadOnlyList<FlashcardResult>>(
            userId, "Flashcards", sourceHash, provider.Provider, provider.Model, mode, null, useCache, regenerate, cancellationToken);
        if (cached is not null)
        {
            return cached with { FromCache = true };
        }

        if (mode == "FreeLocal")
        {
            var local = _localStudyToolService.GenerateFlashcards(content, cardCount);
            await SaveJsonCacheAsync(userId, "Flashcards", sourceHash, provider.Provider, provider.Model, mode, null, local, null, cancellationToken);
            return new StudyGenerationResult<IReadOnlyList<FlashcardResult>>(local, "FreeLocal", null, false, "Generated locally without AI. Quality may be simpler than AI-generated output.");
        }

        try
        {
            var aiCards = await _aiService.GenerateFlashcardsAsync(content, cardCount, cancellationToken);
            await SaveJsonCacheAsync(userId, "Flashcards", sourceHash, provider.Provider, provider.Model, mode, null, aiCards, null, cancellationToken);
            return new StudyGenerationResult<IReadOnlyList<FlashcardResult>>(aiCards, "AIProvider", provider.Provider, false, null);
        }
        catch (Exception ex) when (mode == "Auto" && IsFallbackException(ex))
        {
            _logger.LogWarning(ex, "AI flashcards failed in Auto mode. Falling back to local generation. Provider={Provider}", provider.Provider);
            var local = _localStudyToolService.GenerateFlashcards(content, cardCount);
            await SaveJsonCacheAsync(userId, "Flashcards", sourceHash, provider.Provider, provider.Model, mode, null, local, FallbackNotice, cancellationToken);
            return new StudyGenerationResult<IReadOnlyList<FlashcardResult>>(local, "FreeLocal", provider.Provider, false, FallbackNotice, ToErrorCode(ex));
        }
        catch (Exception ex)
        {
            throw ToProviderException(ex, provider.Provider);
        }
    }

    public async Task<StudyGenerationResult<string>> GenerateStudyTipsAsync(
        Guid userId,
        string content,
        string generationMode,
        bool useCache,
        bool regenerate,
        CancellationToken cancellationToken = default)
    {
        var mode = NormalizeMode(generationMode);
        var provider = await ResolveProviderAsync(userId);
        var sourceHash = Hash(content, "StudyTips", provider.Provider, provider.Model, mode, string.Empty);

        var cached = await TryGetMarkdownCacheAsync<string>(
            userId, "StudyTips", sourceHash, provider.Provider, provider.Model, mode, null, useCache, regenerate, cancellationToken);
        if (cached is not null)
        {
            return cached with { FromCache = true };
        }

        if (mode == "FreeLocal")
        {
            var local = _localStudyToolService.GenerateStudyTips(content);
            await SaveMarkdownCacheAsync(userId, "StudyTips", sourceHash, provider.Provider, provider.Model, mode, null, local, null, cancellationToken);
            return new StudyGenerationResult<string>(local, "FreeLocal", null, false, "Generated locally without AI. Quality may be simpler than AI-generated output.");
        }

        try
        {
            var tips = await _aiService.GetStudyTipsAsync(content, cancellationToken);
            await SaveMarkdownCacheAsync(userId, "StudyTips", sourceHash, provider.Provider, provider.Model, mode, null, tips, null, cancellationToken);
            return new StudyGenerationResult<string>(tips, "AIProvider", provider.Provider, false, null);
        }
        catch (Exception ex) when (mode == "Auto" && IsFallbackException(ex))
        {
            _logger.LogWarning(ex, "AI study tips failed in Auto mode. Falling back to local generation. Provider={Provider}", provider.Provider);
            var local = _localStudyToolService.GenerateStudyTips(content);
            await SaveMarkdownCacheAsync(userId, "StudyTips", sourceHash, provider.Provider, provider.Model, mode, null, local, FallbackNotice, cancellationToken);
            return new StudyGenerationResult<string>(local, "FreeLocal", provider.Provider, false, FallbackNotice, ToErrorCode(ex));
        }
        catch (Exception ex)
        {
            throw ToProviderException(ex, provider.Provider);
        }
    }

    private async Task<(string Provider, string Model)> ResolveProviderAsync(Guid userId)
    {
        var settings = await _unitOfWork.UserAiSettings.GetByUserIdAsync(userId);
        if (settings is not null)
        {
            var provider = NormalizeProvider(settings.ActiveProvider);
            var model = string.IsNullOrWhiteSpace(settings.CustomModel) ? GetDefaultModel(provider) : settings.CustomModel.Trim();
            return (provider, model);
        }

        var (config, _) = _store.Resolve(userId);
        var defaultProvider = NormalizeProvider(config.Provider);
        var defaultModel = string.IsNullOrWhiteSpace(config.Model) ? GetDefaultModel(defaultProvider) : config.Model.Trim();
        return (defaultProvider, defaultModel);
    }

    private async Task<StudyGenerationResult<T>?> TryGetMarkdownCacheAsync<T>(
        Guid userId,
        string taskType,
        string sourceHash,
        string provider,
        string model,
        string mode,
        string? depth,
        bool useCache,
        bool regenerate,
        CancellationToken cancellationToken)
    {
        if (!useCache || regenerate)
        {
            return null;
        }

        var cached = await FindCacheAsync(userId, taskType, sourceHash, provider, model, mode, depth, cancellationToken);
        if (cached?.ResultMarkdown is null)
        {
            return null;
        }

        return new StudyGenerationResult<T>((T)(object)cached.ResultMarkdown, cached.GenerationMode, cached.Provider, true, cached.Notice);
    }

    private async Task<StudyGenerationResult<T>?> TryGetJsonCacheAsync<T>(
        Guid userId,
        string taskType,
        string sourceHash,
        string provider,
        string model,
        string mode,
        string? depth,
        bool useCache,
        bool regenerate,
        CancellationToken cancellationToken)
    {
        if (!useCache || regenerate)
        {
            return null;
        }

        var cached = await FindCacheAsync(userId, taskType, sourceHash, provider, model, mode, depth, cancellationToken);
        if (cached?.ResultJson is null)
        {
            return null;
        }

        var result = JsonSerializer.Deserialize<T>(cached.ResultJson, JsonOptions);
        return result is null
            ? null
            : new StudyGenerationResult<T>(result, cached.GenerationMode, cached.Provider, true, cached.Notice);
    }

    private Task<AiGeneratedContentCache?> FindCacheAsync(
        Guid userId,
        string taskType,
        string sourceHash,
        string provider,
        string model,
        string mode,
        string? depth,
        CancellationToken cancellationToken) =>
        _dbContext.Set<AiGeneratedContentCache>()
            .AsNoTracking()
            .Where(item => item.UserId == userId &&
                           item.TaskType == taskType &&
                           item.SourceHash == sourceHash &&
                           item.Provider == provider &&
                           item.Model == model &&
                           item.GenerationMode == mode &&
                           item.SummaryDepth == depth &&
                           (item.ExpiresAt == null || item.ExpiresAt > DateTime.UtcNow))
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task SaveMarkdownCacheAsync(
        Guid userId,
        string taskType,
        string sourceHash,
        string provider,
        string model,
        string mode,
        string? depth,
        string result,
        string? notice,
        CancellationToken cancellationToken)
    {
        _dbContext.Set<AiGeneratedContentCache>().Add(new AiGeneratedContentCache
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TaskType = taskType,
            SourceHash = sourceHash,
            Provider = provider,
            Model = model,
            GenerationMode = mode,
            SummaryDepth = depth,
            ResultMarkdown = result,
            Notice = notice,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SaveJsonCacheAsync<T>(
        Guid userId,
        string taskType,
        string sourceHash,
        string provider,
        string model,
        string mode,
        string? depth,
        T result,
        string? notice,
        CancellationToken cancellationToken)
    {
        _dbContext.Set<AiGeneratedContentCache>().Add(new AiGeneratedContentCache
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TaskType = taskType,
            SourceHash = sourceHash,
            Provider = provider,
            Model = model,
            GenerationMode = mode,
            SummaryDepth = depth,
            ResultJson = JsonSerializer.Serialize(result, JsonOptions),
            Notice = notice,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string Hash(string content, string taskType, string provider, string model, string mode, string options)
    {
        var normalized = string.Join(" ", (content ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries));
        var source = $"{taskType}|{provider}|{model}|{mode}|{options}|{normalized}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
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

    private static string NormalizeMode(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "aiprovider" or "ai" => "AIProvider",
            "freelocal" or "local" => "FreeLocal",
            _ => "Auto"
        };

    private static string NormalizeDepth(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "quick" => "Quick",
            "detailed" => "Detailed",
            _ => "Balanced"
        };

    private static string NormalizeProvider(string provider) =>
        provider.Trim().ToLowerInvariant() switch
        {
            "openai" => "OpenAI",
            "claude" => "Claude",
            "ollama" => "Ollama",
            "localopenai" or "local openai" or "llama.cpp" => "LocalOpenAI",
            "mock" => "Mock",
            _ => "Gemini"
        };

    private static string BuildSummaryInput(string content, string depth) =>
        "Create a structured markdown study summary with exactly these sections: " +
        "Overview, Key Points, Important Details, Important Terms, Must Remember, Short Revision Summary. " +
        "Do not remove important details such as dates, names, definitions, legal rules, formulas, examples, " +
        "exceptions, lists, classifications, and cause-effect relationships. Compress intelligently and preserve " +
        $"learning-critical information. Summary depth: {depth}.\n\n{content}";

    private static bool IsFallbackException(Exception ex)
    {
        if (ex is AiProviderException providerException)
        {
            return providerException.Code is "AI_RATE_LIMIT" or "AI_CONFIG_MISSING" or "AI_PROVIDER_UNAVAILABLE";
        }

        return ex is InvalidOperationException or HttpRequestException or TaskCanceledException or TimeoutException;
    }

    private static string ToErrorCode(Exception ex)
    {
        if (ex is AiProviderException providerException)
        {
            return providerException.Code;
        }

        var message = ex.Message.ToLowerInvariant();
        if (message.Contains("rate limit") || message.Contains("quota"))
        {
            return "AI_RATE_LIMIT";
        }

        if (message.Contains("api key") || message.Contains("configured"))
        {
            return "AI_CONFIG_MISSING";
        }

        return "AI_PROVIDER_UNAVAILABLE";
    }

    private static AiProviderException ToProviderException(Exception ex, string provider)
    {
        if (ex is AiProviderException aiProviderException)
        {
            return aiProviderException;
        }

        var code = ToErrorCode(ex);
        var status = code switch
        {
            "AI_RATE_LIMIT" => 429,
            "AI_CONFIG_MISSING" => 400,
            _ => 502
        };
        var message = code switch
        {
            "AI_RATE_LIMIT" => $"{provider} quota or rate limit was reached. You can wait, switch to Free Local mode, add your own API key, or use your local LLM server.",
            "AI_CONFIG_MISSING" => $"{provider} API key is not configured. Add it in AI Settings or switch to Free Local mode.",
            _ => $"{provider} provider is unavailable. Check the provider settings or switch to Free Local mode."
        };

        return new AiProviderException(code, message, provider, status, ex.Message, ex);
    }
}
