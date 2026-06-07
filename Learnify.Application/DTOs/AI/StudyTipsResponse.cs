namespace Learnify.Application.DTOs.AI;

/// <summary>
/// Response DTO for study tips generation.
/// </summary>
public record StudyTipsResponse(
    string Tips,
    DateTime GeneratedAt,
    string GenerationModeUsed = "AIProvider",
    string? ProviderUsed = null,
    bool FromCache = false,
    string? Notice = null,
    string? ErrorCode = null);
