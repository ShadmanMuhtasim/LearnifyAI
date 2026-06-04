using Learnify.Application.DTOs;

namespace Learnify.Application.Interfaces;

public interface IQuizService
{
    Task<QuizDto> GenerateQuizAsync(Guid userId, GenerateQuizRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<QuizDto>> GetQuizzesAsync(Guid userId);
    Task<QuizDto?> GetQuizAsync(Guid userId, Guid quizId, bool includeAnswers = false);
    Task<QuizResultDto?> SubmitAttemptAsync(Guid userId, Guid quizId, SubmitQuizAttemptRequest request);
    Task<IReadOnlyList<QuizAttemptDto>> GetAttemptsAsync(Guid userId, Guid quizId);
}
