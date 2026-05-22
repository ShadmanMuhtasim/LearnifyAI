using Learnify.Core.Entities;

namespace Learnify.Core.Interfaces;

/// <summary>
/// Lesson-specific repository interface extending the generic repository.
/// </summary>
public interface ILessonRepository : IRepository<Lesson>
{
    /// <summary>
    /// Finds lessons by a specific course ID asynchronously.
    /// </summary>
    Task<IEnumerable<Lesson>> FindByCourseIdAsync(Guid courseId);

    /// <summary>
    /// Returns the count of lessons for a specific course.
    /// </summary>
    Task<int> CountByCourseIdAsync(Guid courseId);
}