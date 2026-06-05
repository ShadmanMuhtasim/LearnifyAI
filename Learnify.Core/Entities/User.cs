namespace Learnify.Core.Entities;

/// <summary>
/// User entity representing students and instructors with secure password storage.
/// </summary>
public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
   /* public byte[] PasswordSalt { get; set; } = [];*/
    public string Role { get; set; } = "Student"; // Student, Instructor, Admin
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    public virtual ICollection<Course> CreatedCourses { get; set; } = new List<Course>();
    public virtual ICollection<Note> Notes { get; set; } = new List<Note>();
    public virtual ICollection<StudyPlanItem> StudyPlanItems { get; set; } = new List<StudyPlanItem>();
    public virtual UserAiSettings? AiSettings { get; set; }
}
