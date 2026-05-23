namespace Learnify.Application.DTOs.AI;

/// <summary>
/// Response DTO for study tips generation.
/// </summary>
public record StudyTipsResponse(
    string Tips,
    DateTime GeneratedAt);