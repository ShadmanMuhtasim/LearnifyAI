namespace Learnify.Core.Entities;

/// <summary>
/// Base entity class providing common properties for all entities.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}