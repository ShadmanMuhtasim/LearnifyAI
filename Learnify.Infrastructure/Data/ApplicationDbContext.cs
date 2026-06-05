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
    public DbSet<StudyPlanItem> StudyPlanItems { get; set; } = null!;

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

            entity.HasMany(e => e.StudyPlanItems)
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

            entity.HasMany<StudyPlanItem>()
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

            entity.HasMany<StudyPlanItem>()
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

            entity.HasMany<StudyPlanItem>()
                  .WithOne(e => e.Quiz)
                  .HasForeignKey(e => e.QuizId)
                  .OnDelete(DeleteBehavior.NoAction);
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

        modelBuilder.Entity<StudyPlanItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.ScheduledFor });
            entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.PlanType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Priority).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Source).IsRequired().HasMaxLength(50);
            entity.Property(e => e.EstimatedMinutes).IsRequired();
            entity.Property(e => e.ScheduledFor).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

    }
}
