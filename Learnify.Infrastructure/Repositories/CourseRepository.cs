using Learnify.Core.Entities;
using Learnify.Core.Interfaces;
using Learnify.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Learnify.Infrastructure.Repositories;

/// <summary>
/// Entity Framework Core implementation of the Course repository.
/// </summary>
public class CourseRepository : EfRepository<Course>, ICourseRepository
{
    public CourseRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Course>> FindByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Include(c => c.User)
            .Where(c => c.UserId == userId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Course>> SearchByTitleAsync(string searchTerm)
    {
        return await _dbSet
            .Include(c => c.User)
            .Where(c => c.Title.Contains(searchTerm))
            .ToListAsync();
    }

    public async Task<int> CountByUserIdAsync(Guid userId)
    {
        return await _dbSet.CountAsync(c => c.UserId == userId);
    }
}
