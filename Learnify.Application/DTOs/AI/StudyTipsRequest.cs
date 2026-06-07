namespace Learnify.Application.DTOs.AI;

/// <summary>
/// Request DTO for generating study tips for a topic.
/// </summary>
public record StudyTipsRequest(
    string Topic,
    string GenerationMode = "Auto",
    bool UseCache = true,
    bool Regenerate = false);
