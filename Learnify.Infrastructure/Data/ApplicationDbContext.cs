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
    public DbSet<Lesson> Lessons { get; set; } = null!;

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
            entity.HasMany(e => e.Courses)
                  .WithOne(e => e.User)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
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
        });

        // Note entity configuration
        modelBuilder.Entity<Note>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
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

        // Seed Data
        var adminId = Guid.NewGuid();
        var instructorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = adminId,
                FullName = "Shadman Rahman",
                Email = "shadman@learnify.com",
                PasswordHash = "Admin@123",
                Role = "Admin",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id = instructorId,
                FullName = "Fatima Akhtar",
                Email = "fatima@learnify.com",
                PasswordHash = "Instructor@123",
                Role = "Instructor",
                CreatedAt = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id = studentId,
                FullName = "Arif Hossain",
                Email = "arif@learnify.com",
                PasswordHash = "Student@123",
                Role = "Student",
                CreatedAt = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        var course1Id = Guid.NewGuid();
        var course2Id = Guid.NewGuid();
        var course3Id = Guid.NewGuid();

        modelBuilder.Entity<Course>().HasData(
            new Course
            {
                Id = course1Id,
                Title = "Advanced C# Programming",
                Description = "Deep dive into advanced C# features including async/await, reflection, expression trees, and performance optimization techniques.",
                UserId = instructorId,
                CreatedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Course
            {
                Id = course2Id,
                Title = "ASP.NET Core Web API Development",
                Description = "Build robust and scalable RESTful APIs using ASP.NET Core, including authentication, middleware, and best practices.",
                UserId = instructorId,
                CreatedAt = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc)
            },
            new Course
            {
                Id = course3Id,
                Title = "Entity Framework Core Masterclass",
                Description = "Master EF Core including migrations, relationships, query tracking, change tracking, and performance tuning for enterprise applications.",
                UserId = instructorId,
                CreatedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        var note1Id = Guid.NewGuid();
        var note2Id = Guid.NewGuid();
        var note3Id = Guid.NewGuid();

        modelBuilder.Entity<Note>().HasData(
            new Note
            {
                Id = note1Id,
                Content = "Key takeaway: Use 'await' consistently and avoid .Result to prevent deadlocks. Always prefer async all the way down.",
                CourseId = course1Id,
                CreatedAt = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc)
            },
            new Note
            {
                Id = note2Id,
                Content = "Remember: API versioning is critical for enterprise apps. Consider URL path versioning for simplicity and HTTP header versioning for flexibility.",
                CourseId = course2Id,
                CreatedAt = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc)
            },
            new Note
            {
                Id = note3Id,
                Content = "EF Core Performance Tip: Use AsNoTracking() for read-only queries to avoid change tracker overhead. Profile with SQL Server Profiler.",
                CourseId = course3Id,
                CreatedAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
