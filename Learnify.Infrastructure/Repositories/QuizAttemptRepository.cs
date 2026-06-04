using Learnify.Core.Entities;
using Learnify.Core.Interfaces;
using Learnify.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Learnify.Infrastructure.Repositories;

public class QuizAttemptRepository : EfRepository<QuizAttempt>, IQuizAttemptRepository
{
    public QuizAttemptRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<QuizAttempt>> FindByQuizAndUserAsync(Guid quizId, Guid userId)
    {
        return await _context.QuizAttempts
            .AsNoTracking()
            .Include(a => a.Answers)
                .ThenInclude(answer => answer.Question)
            .Where(a => a.QuizId == quizId && a.UserId == userId)
            .OrderByDescending(a => a.CompletedAt ?? a.StartedAt)
            .ToListAsync();
    }

    public async Task<QuizAttempt?> GetOwnedAttemptAsync(Guid attemptId, Guid userId)
    {
        return await _context.QuizAttempts
            .Include(a => a.Answers)
                .ThenInclude(answer => answer.Question)
            .Include(a => a.Quiz)
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId);
    }
}
