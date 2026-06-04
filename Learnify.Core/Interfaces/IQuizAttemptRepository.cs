using Learnify.Core.Entities;

namespace Learnify.Core.Interfaces;

public interface IQuizAttemptRepository : IRepository<QuizAttempt>
{
    Task<IEnumerable<QuizAttempt>> FindByQuizAndUserAsync(Guid quizId, Guid userId);
    Task<QuizAttempt?> GetOwnedAttemptAsync(Guid attemptId, Guid userId);
}
