using Learnify.Core.Entities;
using Learnify.Core.Interfaces;
using Learnify.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Learnify.Infrastructure.Repositories;

public class QuizRepository : EfRepository<Quiz>, IQuizRepository
{
    public QuizRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Quiz>> FindByUserIdAsync(Guid userId)
    {
        return await _context.Quizzes
            .AsNoTracking()
            .Include(q => q.Questions.OrderBy(question => question.OrderIndex))
            .Where(q => q.UserId == userId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
    }

    public async Task<Quiz?> GetOwnedQuizAsync(Guid quizId, Guid userId)
    {
        return await _context.Quizzes
            .Include(q => q.Questions.OrderBy(question => question.OrderIndex))
            .Include(q => q.Attempts)
                .ThenInclude(a => a.Answers)
            .FirstOrDefaultAsync(q => q.Id == quizId && q.UserId == userId);
    }
}
