namespace Learnify.Core.Entities;

/// <summary>
/// Per-user AI provider preferences.
/// API keys are stored server-side and are never returned by API responses.
/// </summary>
public class UserAiSettings : BaseEntity
{
    public Guid UserId { get; set; }
    public string ActiveProvider { get; set; } = "Gemini";
    public string? ApiKey { get; set; }
    public string? CustomModel { get; set; }
    public string? OllamaBaseUrl { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
