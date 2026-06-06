using Learnify.Application.DTOs;
using Learnify.Application.Interfaces;
using Learnify.Core.Entities;
using Learnify.Core.Interfaces;
using System.Text.Json;

namespace Learnify.Application.Services;

public class QuizService : IQuizService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAiService _aiService;

    public QuizService(IUnitOfWork unitOfWork, IAiService aiService)
    {
        _unitOfWork = unitOfWork;
        _aiService = aiService;
    }

    public async Task<QuizDto> GenerateQuizAsync(Guid userId, GenerateQuizRequest request, CancellationToken ct = default)
    {
        if (request.NoteId == Guid.Empty)
        {
            throw new InvalidOperationException("A note is required to generate a quiz.");
        }

        var note = await _unitOfWork.Notes.GetByIdAsync(request.NoteId);
        if (note == null)
        {
            throw new UnauthorizedAccessException("Note not found.");
        }

        var course = await _unitOfWork.Courses.GetByIdAsync(note.CourseId);
        if (course == null || course.UserId != userId)
        {
            throw new UnauthorizedAccessException("Note not found.");
        }

        if (string.IsNullOrWhiteSpace(note.Content))
        {
            throw new InvalidOperationException("The selected note does not contain content for quiz generation.");
        }

        var questionTypes = NormalizeQuestionTypes(request.QuestionTypes);
        var difficulty = NormalizeDifficulty(request.Difficulty);
        var numberOfQuestions = Math.Clamp(request.NumberOfQuestions, 1, 20);
        var generated = await _aiService.GenerateQuizAsync(
            note.Content,
            questionTypes,
            difficulty,
            numberOfQuestions,
            ct);

        var validQuestions = generated.Questions
            .Where(q => !string.IsNullOrWhiteSpace(q.QuestionText) &&
                        !string.IsNullOrWhiteSpace(q.CorrectAnswer))
            .Take(numberOfQuestions)
            .ToList();

        if (validQuestions.Count == 0)
        {
            throw new InvalidOperationException("AI did not return any valid quiz questions.");
        }

        if (validQuestions.Count < numberOfQuestions)
        {
            throw new InvalidOperationException("The AI provider could not generate the requested quiz size. Try fewer questions or switch provider.");
        }

        var quiz = new Quiz
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CourseId = course.Id,
            NoteId = note.Id,
            Title = string.IsNullOrWhiteSpace(generated.Title) ? "Generated Quiz" : generated.Title.Trim(),
            Description = $"Generated from note in {course.Title}.",
            Difficulty = difficulty,
            QuestionTypes = string.Join(",", questionTypes),
            TimeLimitMinutes = NormalizeTimeLimit(request.TimeLimitMinutes),
            CreatedAt = DateTime.UtcNow
        };

        for (var index = 0; index < validQuestions.Count; index++)
        {
            var question = validQuestions[index];
            var type = NormalizeQuestionType(question.Type);
            var options = type == "TrueFalse"
                ? new List<string> { "True", "False" }
                : question.Options.Where(option => !string.IsNullOrWhiteSpace(option)).ToList();

            quiz.Questions.Add(new Question
            {
                Id = Guid.NewGuid(),
                QuizId = quiz.Id,
                Type = type,
                QuestionText = question.QuestionText.Trim(),
                OptionsJson = options.Count > 0 ? JsonSerializer.Serialize(options) : null,
                CorrectAnswer = question.CorrectAnswer.Trim(),
                Explanation = string.IsNullOrWhiteSpace(question.Explanation) ? null : question.Explanation.Trim(),
                Points = 1,
                OrderIndex = index,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _unitOfWork.Quizzes.AddAsync(quiz);
        await _unitOfWork.SaveChangesAsync();

        return ToQuizDto(quiz, includeAnswers: true);
    }

    public async Task<IReadOnlyList<QuizDto>> GetQuizzesAsync(Guid userId)
    {
        var quizzes = await _unitOfWork.Quizzes.FindByUserIdAsync(userId);
        return quizzes.Select(q => ToQuizDto(q, includeAnswers: false)).ToList();
    }

    public async Task<QuizDto?> GetQuizAsync(Guid userId, Guid quizId, bool includeAnswers = false)
    {
        var quiz = await _unitOfWork.Quizzes.GetOwnedQuizAsync(quizId, userId);
        return quiz == null ? null : ToQuizDto(quiz, includeAnswers);
    }

    public async Task<QuizResultDto?> SubmitAttemptAsync(
        Guid userId,
        Guid quizId,
        SubmitQuizAttemptRequest request)
    {
        if (request.Answers.Count == 0)
        {
            throw new InvalidOperationException("Submit at least one answer.");
        }

        var quiz = await _unitOfWork.Quizzes.GetOwnedQuizAsync(quizId, userId);
        if (quiz == null)
        {
            return null;
        }

        var questions = quiz.Questions.OrderBy(q => q.OrderIndex).ToList();
        if (questions.Count == 0)
        {
            throw new InvalidOperationException("Quiz has no questions.");
        }

        var submitted = request.Answers
            .GroupBy(answer => answer.QuestionId)
            .ToDictionary(group => group.Key, group => group.Last().UserAnswer ?? string.Empty);
        var now = DateTime.UtcNow;
        var attempt = new QuizAttempt
        {
            Id = Guid.NewGuid(),
            QuizId = quiz.Id,
            UserId = userId,
            StartedAt = now,
            CompletedAt = now,
            CreatedAt = now,
            TotalPoints = questions.Sum(q => q.Points)
        };

        foreach (var question in questions)
        {
            submitted.TryGetValue(question.Id, out var userAnswer);
            userAnswer = userAnswer?.Trim() ?? string.Empty;
            var isCorrect = IsCorrectAnswer(question, userAnswer);
            var pointsAwarded = isCorrect ? question.Points : 0;

            attempt.Score += pointsAwarded;
            attempt.Answers.Add(new QuizAttemptAnswer
            {
                Id = Guid.NewGuid(),
                QuizAttemptId = attempt.Id,
                QuestionId = question.Id,
                UserAnswer = userAnswer,
                IsCorrect = isCorrect,
                PointsAwarded = pointsAwarded,
                CreatedAt = now
            });
        }

        attempt.Percentage = attempt.TotalPoints == 0
            ? 0
            : Math.Round((decimal)attempt.Score / attempt.TotalPoints * 100, 2);

        await _unitOfWork.QuizAttempts.AddAsync(attempt);
        await _unitOfWork.SaveChangesAsync();

        foreach (var answer in attempt.Answers)
        {
            answer.Question = questions.First(q => q.Id == answer.QuestionId);
        }

        return ToResultDto(attempt);
    }

    public async Task<IReadOnlyList<QuizAttemptDto>> GetAttemptsAsync(Guid userId, Guid quizId)
    {
        var quiz = await _unitOfWork.Quizzes.GetOwnedQuizAsync(quizId, userId);
        if (quiz == null)
        {
            return new List<QuizAttemptDto>();
        }

        var attempts = await _unitOfWork.QuizAttempts.FindByQuizAndUserAsync(quizId, userId);
        return attempts.Select(ToAttemptDto).ToList();
    }

    private static bool IsCorrectAnswer(Question question, string userAnswer)
    {
        if (string.IsNullOrWhiteSpace(userAnswer))
        {
            return false;
        }

        return NormalizeAnswer(userAnswer).Equals(
            NormalizeAnswer(question.CorrectAnswer),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeAnswer(string value) =>
        string.Join(" ", value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static QuizDto ToQuizDto(Quiz quiz, bool includeAnswers)
    {
        return new QuizDto
        {
            Id = quiz.Id,
            UserId = quiz.UserId,
            CourseId = quiz.CourseId,
            NoteId = quiz.NoteId,
            Title = quiz.Title,
            Description = quiz.Description,
            Difficulty = quiz.Difficulty,
            TimeLimitMinutes = quiz.TimeLimitMinutes,
            QuestionTypes = quiz.QuestionTypes.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            Questions = quiz.Questions
                .OrderBy(q => q.OrderIndex)
                .Select(q => ToQuestionDto(q, includeAnswers))
                .ToList(),
            CreatedAt = quiz.CreatedAt,
            UpdatedAt = quiz.UpdatedAt
        };
    }

    private static QuestionDto ToQuestionDto(Question question, bool includeAnswers)
    {
        return new QuestionDto
        {
            Id = question.Id,
            Type = question.Type,
            QuestionText = question.QuestionText,
            Options = DeserializeOptions(question.OptionsJson),
            CorrectAnswer = includeAnswers ? question.CorrectAnswer : null,
            Explanation = includeAnswers ? question.Explanation : null,
            Points = question.Points,
            OrderIndex = question.OrderIndex
        };
    }

    private static QuizAttemptDto ToAttemptDto(QuizAttempt attempt)
    {
        return new QuizAttemptDto
        {
            Id = attempt.Id,
            QuizId = attempt.QuizId,
            UserId = attempt.UserId,
            Score = attempt.Score,
            TotalPoints = attempt.TotalPoints,
            Percentage = attempt.Percentage,
            StartedAt = attempt.StartedAt,
            CompletedAt = attempt.CompletedAt
        };
    }

    private static QuizResultDto ToResultDto(QuizAttempt attempt)
    {
        return new QuizResultDto
        {
            AttemptId = attempt.Id,
            QuizId = attempt.QuizId,
            Score = attempt.Score,
            TotalPoints = attempt.TotalPoints,
            Percentage = attempt.Percentage,
            CompletedAt = attempt.CompletedAt ?? DateTime.UtcNow,
            Answers = attempt.Answers
                .OrderBy(answer => answer.Question.OrderIndex)
                .Select(answer => new QuizResultAnswerDto
                {
                    QuestionId = answer.QuestionId,
                    Type = answer.Question.Type,
                    QuestionText = answer.Question.QuestionText,
                    Options = DeserializeOptions(answer.Question.OptionsJson),
                    UserAnswer = answer.UserAnswer,
                    CorrectAnswer = answer.Question.CorrectAnswer,
                    Explanation = answer.Question.Explanation,
                    IsCorrect = answer.IsCorrect,
                    PointsAwarded = answer.PointsAwarded,
                    Points = answer.Question.Points
                })
                .ToList()
        };
    }

    private static List<string> DeserializeOptions(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(optionsJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static List<string> NormalizeQuestionTypes(List<string> questionTypes)
    {
        var normalized = questionTypes
            .Select(NormalizeQuestionType)
            .Where(type => type is "MultipleChoice" or "TrueFalse" or "ShortAnswer" or "FillInTheBlank")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return normalized.Count > 0 ? normalized : new List<string> { "MultipleChoice" };
    }

    private static string NormalizeQuestionType(string type)
    {
        var normalized = type.Replace(" ", "", StringComparison.OrdinalIgnoreCase)
            .Replace("-", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        return normalized.ToLowerInvariant() switch
        {
            "truefalse" => "TrueFalse",
            "shortanswer" => "ShortAnswer",
            "fillintheblank" => "FillInTheBlank",
            "fillblank" => "FillInTheBlank",
            _ => "MultipleChoice"
        };
    }

    private static string NormalizeDifficulty(string difficulty) =>
        difficulty.Trim().ToLowerInvariant() switch
        {
            "easy" => "Easy",
            "hard" => "Hard",
            _ => "Medium"
        };

    private static int? NormalizeTimeLimit(int? timeLimitMinutes)
    {
        if (timeLimitMinutes is null or <= 0)
        {
            return null;
        }

        return Math.Clamp(timeLimitMinutes.Value, 1, 240);
    }
}
