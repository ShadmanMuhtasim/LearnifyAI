using Learnify.Core.Entities;

namespace Learnify.Core.Interfaces;

public interface IQuizRepository : IRepository<Quiz>
{
    Task<IEnumerable<Quiz>> FindByUserIdAsync(Guid userId);
    Task<Quiz?> GetOwnedQuizAsync(Guid quizId, Guid userId);
}
