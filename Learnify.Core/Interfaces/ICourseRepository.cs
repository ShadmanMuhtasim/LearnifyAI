using Learnify.Core.Entities;

namespace Learnify.Core.Interfaces;

/// <summary>
/// Course-specific repository interface extending the generic repository.
/// </summary>
public interface ICourseRepository : IRepository<Course>
{
    /// <summary>
    /// Finds courses by a specific user asynchronously.
    /// </summary>
    Task<IEnumerable<Course>> FindByUserIdAsync(Guid userId);

    /// <summary>
    /// Searches courses by title substring asynchronously.
    /// </summary>
    Task<IEnumerable<Course>> SearchByTitleAsync(string searchTerm);

    /// <summary>
    /// Returns the count of courses for a specific user.
    /// </summary>
    Task<int> CountByUserIdAsync(Guid userId);
}