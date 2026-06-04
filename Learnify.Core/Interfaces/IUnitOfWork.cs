using Learnify.Core.Entities;

namespace Learnify.Core.Interfaces;

/// <summary>
/// Unit of Work interface that coordinates repositories and manages transactions.
/// Ensures all repository operations are committed together or rolled back together.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Gets the User repository.
    /// </summary>
    IUserRepository Users { get; }

    /// <summary>
    /// Gets the Course repository.
    /// </summary>
    ICourseRepository Courses { get; }

    /// <summary>
    /// Gets the Note repository.
    /// </summary>
    INoteRepository Notes { get; }

    /// <summary>
    /// Gets the Lesson repository.
    /// </summary>
    ILessonRepository Lessons { get; }

    /// <summary>
    /// Gets the per-user AI settings repository.
    /// </summary>
    IUserAiSettingsRepository UserAiSettings { get; }

    /// <summary>
    /// Gets the Quiz repository.
    /// </summary>
    IQuizRepository Quizzes { get; }

    /// <summary>
    /// Gets the QuizAttempt repository.
    /// </summary>
    IQuizAttemptRepository QuizAttempts { get; }

    /// <summary>
    /// Saves all pending changes to the database within a transaction.
    /// </summary>
    Task<int> SaveChangesAsync();

    /// <summary>
    /// Begins a new database transaction.
    /// </summary>
    Task BeginTransactionAsync();

    /// <summary>
    /// Commits the current database transaction.
    /// </summary>
    Task CommitTransactionAsync();

    /// <summary>
    /// Rolls back the current database transaction.
    /// </summary>
    void RollbackTransaction();
}
