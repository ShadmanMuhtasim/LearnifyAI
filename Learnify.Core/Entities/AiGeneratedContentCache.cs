namespace Learnify.Core.Entities;

/// <summary>
/// User-scoped cache for successful AI or local study tool output.
/// </summary>
public class AiGeneratedContentCache : BaseEntity
{
    public Guid UserId { get; set; }
    public string TaskType { get; set; } = string.Empty;
    public string SourceHash { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string GenerationMode { get; set; } = string.Empty;
    public string? SummaryDepth { get; set; }
    public string? ResultJson { get; set; }
    public string? ResultMarkdown { get; set; }
    public string? Notice { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public virtual User User { get; set; } = null!;
}
