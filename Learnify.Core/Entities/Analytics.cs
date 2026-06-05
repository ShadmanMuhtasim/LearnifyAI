namespace Learnify.Core.Entities;

public class LearningActivity : BaseEntity
{
    public Guid UserId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public int Points { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string? MetadataJson { get; set; }

    public virtual User User { get; set; } = null!;
}

public class Achievement : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int RequiredValue { get; set; }
    public string AchievementType { get; set; } = string.Empty;
    public int PointsReward { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
}

public class UserAchievement : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid AchievementId { get; set; }
    public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;

    public virtual User User { get; set; } = null!;
    public virtual Achievement Achievement { get; set; } = null!;
}
