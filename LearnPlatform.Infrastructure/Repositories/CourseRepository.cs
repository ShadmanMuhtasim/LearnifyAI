using LearnPlatform.Core.Entities;
using LearnPlatform.Core.Interfaces;
using LearnPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnPlatform.Infrastructure.Repositories;

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
        return await _dbSet.Where(c => c.UserId == userId).ToListAsync();
    }

    public async Task<IEnumerable<Course>> SearchByTitleAsync(string searchTerm)
    {
        return await _dbSet
            .Where(c => c.Title.Contains(searchTerm))
            .ToListAsync();
    }

    public async Task<int> CountByUserIdAsync(Guid userId)
    {
        return await _dbSet.CountAsync(c => c.UserId == userId);
    }
}