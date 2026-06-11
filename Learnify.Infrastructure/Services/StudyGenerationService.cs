using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Learnify.Core.Interfaces;
using Learnify.Core.Models;
using Microsoft.Extensions.Logging;

namespace Learnify.Infrastructure.Services;

public sealed class StudyGenerationService : IStudyGenerationService
{
    private const string ModeAuto = "Auto";
    private const string ModeAiProvider = "AIProvider";
    private const string ModeFreeLocal = "FreeLocal";
    private const string LocalNotice = "Generated locally with Free Local study tools. No AI provider was called.";
    private const string FallbackNotice = "AI provider was unavailable, so Learnify generated this locally with Free Local study tools.";

    private readonly IAiService _aiService;
    private readonly ILocalStudyToolService _localStudyToolService;
    private readonly ILogger<StudyGenerationService> _logger;
    private static readonly ConcurrentDictionary<string, StudyGenerationResult> Cache = new();

    public StudyGenerationService(
        IAiService aiService,
        ILocalStudyToolService localStudyToolService,
        ILogger<StudyGenerationService> logger)
    {
        _aiService = aiService;
        _localStudyToolService = localStudyToolService;
        _logger = logger;
    }

    public Task<StudyGenerationResult> GenerateSummaryAsync(
        Guid userId,
        string noteId,
        string content,
        string? generationMode,
        CancellationToken cancellationToken = default)
        => GenerateAsync(
            userId,
            noteId,
            "summary",
            content,
            generationMode,
            async ct => new StudyGenerationResult(noteId, await _aiService.SummarizeNoteAsync(content, ct), null, null, DateTime.UtcNow, ModeAiProvider, ModeAiProvider, null, false),
            () => new StudyGenerationResult(noteId, _localStudyToolService.Summarize(content), null, null, DateTime.UtcNow, ModeFreeLocal, null, LocalNotice, false),
            cancellationToken);

    public Task<StudyGenerationResult> GenerateFlashcardsAsync(
        Guid userId,
        string noteId,
        string content,
        int count,
        string? generationMode,
        CancellationToken cancellationToken = default)
        => GenerateAsync(
            userId,
            noteId,
            "flashcards",
            $"{count}\n{content}",
            generationMode,
            async ct => new StudyGenerationResult(noteId, null, await _aiService.GenerateFlashcardsAsync(content, count, ct), null, DateTime.UtcNow, ModeAiProvider, ModeAiProvider, null, false),
            () => new StudyGenerationResult(noteId, null, _localStudyToolService.GenerateFlashcards(content, count), null, DateTime.UtcNow, ModeFreeLocal, null, LocalNotice, false),
            cancellationToken);

    public Task<StudyGenerationResult> GenerateStudyTipsAsync(
        Guid userId,
        string topic,
        string? generationMode,
        CancellationToken cancellationToken = default)
        => GenerateAsync(
            userId,
            null,
            "study-tips",
            topic,
            generationMode,
            async ct => new StudyGenerationResult(null, null, null, await _aiService.GetStudyTipsAsync(topic, ct), DateTime.UtcNow, ModeAiProvider, ModeAiProvider, null, false),
            () => new StudyGenerationResult(null, null, null, _localStudyToolService.GenerateStudyTips(topic), DateTime.UtcNow, ModeFreeLocal, null, LocalNotice, false),
            cancellationToken);

    private async Task<StudyGenerationResult> GenerateAsync(
        Guid userId,
        string? noteId,
        string tool,
        string contentForCache,
        string? generationMode,
        Func<CancellationToken, Task<StudyGenerationResult>> aiFactory,
        Func<StudyGenerationResult> localFactory,
        CancellationToken cancellationToken)
    {
        var mode = NormalizeMode(generationMode);
        if (mode == ModeFreeLocal)
        {
            return GetOrCreateLocal(userId, noteId, tool, contentForCache, localFactory, LocalNotice);
        }

        if (mode == ModeAiProvider)
        {
            return await aiFactory(cancellationToken);
        }

        try
        {
            return await aiFactory(cancellationToken);
        }
        catch (Exception ex) when (IsSafeProviderFailure(ex))
        {
            _logger.LogWarning(ex, "AI provider failed for {Tool}; falling back to Free Local generation.", tool);
            return GetOrCreateLocal(userId, noteId, tool, contentForCache, localFactory, FallbackNotice);
        }
    }

    private StudyGenerationResult GetOrCreateLocal(
        Guid userId,
        string? noteId,
        string tool,
        string contentForCache,
        Func<StudyGenerationResult> localFactory,
        string notice)
    {
        var key = BuildCacheKey(userId, noteId, tool, contentForCache);
        if (Cache.TryGetValue(key, out var cached))
        {
            return cached with { FromCache = true, Notice = notice };
        }

        var result = localFactory() with { Notice = notice, FromCache = false };
        Cache[key] = result;
        return result;
    }

    private static string NormalizeMode(string? generationMode)
        => generationMode?.Trim().ToLowerInvariant() switch
        {
            "freelocal" or "free local" or "local" => ModeFreeLocal,
            "aiprovider" or "ai provider" or "ai" => ModeAiProvider,
            _ => ModeAuto
        };

    private static bool IsSafeProviderFailure(Exception ex)
        => ex is InvalidOperationException or TimeoutException or HttpRequestException or TaskCanceledException;

    private static string BuildCacheKey(Guid userId, string? noteId, string tool, string content)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
        return $"{userId:N}:{noteId ?? "none"}:{tool}:{hash}";
    }
}
