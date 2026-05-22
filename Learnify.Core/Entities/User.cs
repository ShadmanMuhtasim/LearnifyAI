namespace Learnify.Core.Entities;

/// <summary>
/// User entity representing students and instructors.
/// </summary>
public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
}