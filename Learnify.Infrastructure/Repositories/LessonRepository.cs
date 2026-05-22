using Learnify.Core.Entities;
using Learnify.Core.Interfaces;
using Learnify.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Learnify.Infrastructure.Repositories;

/// <summary>
/// Lesson repository implementation using Entity Framework Core.
/// </summary>
public class LessonRepository : EfRepository<Lesson>, ILessonRepository
{
    public LessonRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Finds lessons by a specific course ID asynchronously.
    /// </summary>
    public async Task<IEnumerable<Lesson>> FindByCourseIdAsync(Guid courseId)
    {
        return await _context.Lessons
            .Where(l => l.CourseId == courseId)
            .OrderBy(l => l.Order)
            .ToListAsync();
    }

    /// <summary>
    /// Returns the count of lessons for a specific course.
    /// </summary>
    public async Task<int> CountByCourseIdAsync(Guid courseId)
    {
        return await _context.Lessons
            .Where(l => l.CourseId == courseId)
            .CountAsync();
    }
}