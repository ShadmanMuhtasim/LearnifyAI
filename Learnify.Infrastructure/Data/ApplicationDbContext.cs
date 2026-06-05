using Learnify.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Learnify.Infrastructure.Data;

/// <summary>
/// Application database context for managing database access and entity relationships.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Course> Courses { get; set; } = null!;
    public DbSet<Note> Notes { get; set; } = null!;
    public DbSet<NoteAttachment> NoteAttachments { get; set; } = null!;
    public DbSet<Lesson> Lessons { get; set; } = null!;
    public DbSet<UserAiSettings> UserAiSettings { get; set; } = null!;
    public DbSet<Quiz> Quizzes { get; set; } = null!;
    public DbSet<Question> Questions { get; set; } = null!;
    public DbSet<QuizAttempt> QuizAttempts { get; set; } = null!;
    public DbSet<QuizAttemptAnswer> QuizAttemptAnswers { get; set; } = null!;
    public DbSet<LearningActivity> LearningActivities { get; set; } = null!;
    public DbSet<Achievement> Achievements { get; set; } = null!;
    public DbSet<UserAchievement> UserAchievements { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User entity configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // User has many Courses (1-to-many)
            entity.HasMany(e => e.CreatedCourses)
                  .WithOne(e => e.User)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.AiSettings)
                  .WithOne(e => e.User)
                  .HasForeignKey<UserAiSettings>(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany<Quiz>()
                  .WithOne(e => e.User)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasMany<QuizAttempt>()
                  .WithOne(e => e.User)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasMany<LearningActivity>()
                  .WithOne(e => e.User)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany<UserAchievement>()
                  .WithOne(e => e.User)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserAiSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.ActiveProvider).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ApiKey).HasMaxLength(4000);
            entity.Property(e => e.CustomModel).HasMaxLength(200);
            entity.Property(e => e.OllamaBaseUrl).HasMaxLength(500);
            entity.Property(e => e.LocalOpenAiBaseUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // Course entity configuration
        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Course has many Notes (1-to-many)
            entity.HasMany(e => e.Notes)
                  .WithOne(e => e.Course)
                  .HasForeignKey(e => e.CourseId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany<Quiz>()
                  .WithOne(e => e.Course)
                  .HasForeignKey(e => e.CourseId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // Note entity configuration
        modelBuilder.Entity<Note>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.AttachmentName).HasMaxLength(500);
            entity.Property(e => e.AttachmentType).HasMaxLength(200);
            entity.Property(e => e.AttachmentBase64);

            entity.HasMany<Quiz>()
                  .WithOne(e => e.Note)
                  .HasForeignKey(e => e.NoteId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // NoteAttachment entity configuration
        modelBuilder.Entity<NoteAttachment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Base64).IsRequired();
            entity.HasOne(e => e.Note)
                  .WithMany(e => e.Attachments)
                  .HasForeignKey(e => e.NoteId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Lesson entity configuration
        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Lesson belongs to Course
            entity.HasOne(e => e.Course)
                  .WithMany()
                  .HasForeignKey(e => e.CourseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Difficulty).IsRequired().HasMaxLength(50);
            entity.Property(e => e.QuestionTypes).IsRequired().HasMaxLength(500);
            entity.Property(e => e.TimeLimitMinutes);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            entity.HasMany(e => e.Questions)
                  .WithOne(e => e.Quiz)
                  .HasForeignKey(e => e.QuizId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Attempts)
                  .WithOne(e => e.Quiz)
                  .HasForeignKey(e => e.QuizId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.QuestionText).IsRequired();
            entity.Property(e => e.OptionsJson);
            entity.Property(e => e.CorrectAnswer).IsRequired();
            entity.Property(e => e.Explanation);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        modelBuilder.Entity<QuizAttempt>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Percentage).HasPrecision(5, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.StartedAt).HasDefaultValueSql("GETUTCDATE()");

            entity.HasMany(e => e.Answers)
                  .WithOne(e => e.QuizAttempt)
                  .HasForeignKey(e => e.QuizAttemptId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuizAttemptAnswer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserAnswer).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.Question)
                  .WithMany()
                  .HasForeignKey(e => e.QuestionId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<LearningActivity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.OccurredAt });
            entity.Property(e => e.ActivityType).IsRequired().HasMaxLength(80);
            entity.Property(e => e.EntityType).HasMaxLength(80);
            entity.Property(e => e.MetadataJson);
            entity.Property(e => e.OccurredAt).HasDefaultValueSql("GETUTCDATE()");
        });

        modelBuilder.Entity<Achievement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Code).IsRequired().HasMaxLength(80);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(160);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Icon).HasMaxLength(40);
            entity.Property(e => e.AchievementType).IsRequired().HasMaxLength(80);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasData(AchievementDefinitions);
        });

        modelBuilder.Entity<UserAchievement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.AchievementId }).IsUnique();
            entity.Property(e => e.UnlockedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasOne(e => e.Achievement)
                  .WithMany(e => e.UserAchievements)
                  .HasForeignKey(e => e.AchievementId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static readonly Achievement[] AchievementDefinitions =
    [
        new()
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111101"),
            Code = "first-course",
            Title = "First Course",
            Description = "Create your first course.",
            Icon = "CR",
            RequiredValue = 1,
            AchievementType = "CourseCreated",
            PointsReward = 25,
            IsActive = true,
            CreatedAt = new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111102"),
            Code = "first-note",
            Title = "First Note",
            Description = "Upload or create your first note.",
            Icon = "NT",
            RequiredValue = 1,
            AchievementType = "NoteUploaded",
            PointsReward = 25,
            IsActive = true,
            CreatedAt = new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111103"),
            Code = "first-quiz",
            Title = "First Quiz",
            Description = "Generate your first quiz.",
            Icon = "QZ",
            RequiredValue = 1,
            AchievementType = "QuizGenerated",
            PointsReward = 30,
            IsActive = true,
            CreatedAt = new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111104"),
            Code = "perfect-quiz",
            Title = "Perfect Quiz",
            Description = "Score 100% on a quiz attempt.",
            Icon = "100",
            RequiredValue = 1,
            AchievementType = "PerfectQuiz",
            PointsReward = 50,
            IsActive = true,
            CreatedAt = new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111105"),
            Code = "flashcard-starter",
            Title = "Flashcard Starter",
            Description = "Generate flashcards from your notes.",
            Icon = "FC",
            RequiredValue = 1,
            AchievementType = "FlashcardsGenerated",
            PointsReward = 25,
            IsActive = true,
            CreatedAt = new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111106"),
            Code = "ai-explorer",
            Title = "AI Explorer",
            Description = "Use summary, flashcards, and study tips.",
            Icon = "AI",
            RequiredValue = 3,
            AchievementType = "AiToolKindsUsed",
            PointsReward = 40,
            IsActive = true,
            CreatedAt = new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111107"),
            Code = "three-day-learner",
            Title = "Three Day Learner",
            Description = "Record learning activity on three separate days.",
            Icon = "ST",
            RequiredValue = 3,
            AchievementType = "LongestStreak",
            PointsReward = 45,
            IsActive = true,
            CreatedAt = new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111108"),
            Code = "productive-learner",
            Title = "Productive Learner",
            Description = "Complete five learning actions.",
            Icon = "XP",
            RequiredValue = 5,
            AchievementType = "ActivityCount",
            PointsReward = 35,
            IsActive = true,
            CreatedAt = new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc)
        }
    ];
}
