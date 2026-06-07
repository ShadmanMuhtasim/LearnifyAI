namespace Learnify.Core.Models;

public sealed record StudyGenerationResult<T>(
    T Result,
    string GenerationModeUsed,
    string? ProviderUsed,
    bool FromCache,
    string? Notice,
    string? ErrorCode = null);
