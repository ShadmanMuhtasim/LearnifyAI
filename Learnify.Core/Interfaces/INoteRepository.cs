using Learnify.Core.Entities;

namespace Learnify.Core.Interfaces;

/// <summary>
/// Note-specific repository interface extending the generic repository.
/// </summary>
public interface INoteRepository : IRepository<Note>
{
    /// <summary>
    /// Finds notes by course ID asynchronously.
    /// </summary>
    Task<IEnumerable<Note>> FindByCourseIdAsync(Guid courseId);

    /// <summary>
    /// Finds notes by user ID (via course relationship) asynchronously.
    /// </summary>
    Task<IEnumerable<Note>> FindByUserIdAsync(Guid userId);

    /// <summary>
    /// Returns the count of notes for a specific course.
    /// </summary>
    Task<int> CountByCourseIdAsync(Guid courseId);
}