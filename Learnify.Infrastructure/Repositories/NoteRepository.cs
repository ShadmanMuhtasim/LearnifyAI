using Learnify.Core.Entities;
using Learnify.Core.Interfaces;
using Learnify.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Learnify.Infrastructure.Repositories;

/// <summary>
/// Entity Framework Core implementation of the Note repository.
/// </summary>
public class NoteRepository : EfRepository<Note>, INoteRepository
{
    public NoteRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Note>> FindByCourseIdAsync(Guid courseId)
    {
        return await _dbSet.Where(n => n.CourseId == courseId).ToListAsync();
    }

    public async Task<IEnumerable<Note>> FindByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Include(n => n.Course)
            .Where(n => n.Course.UserId == userId)
            .ToListAsync();
    }

    public async Task<int> CountByCourseIdAsync(Guid courseId)
    {
        return await _dbSet.CountAsync(n => n.CourseId == courseId);
    }
}