namespace Learnify.Application.DTOs;

public class GenerateQuizRequest
{
    public Guid NoteId { get; set; }
    public int NumberOfQuestions { get; set; } = 5;
    public string Difficulty { get; set; } = "Medium";
    public List<string> QuestionTypes { get; set; } = new() { "MultipleChoice" };
    public int? TimeLimitMinutes { get; set; }
}

public class CreateQuizRequest
{
    public Guid? CourseId { get; set; }
    public Guid? NoteId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Difficulty { get; set; } = "Medium";
    public int? TimeLimitMinutes { get; set; }
    public List<QuestionDto> Questions { get; set; } = new();
}

public class QuizDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? CourseId { get; set; }
    public Guid? NoteId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Difficulty { get; set; } = "Medium";
    public int? TimeLimitMinutes { get; set; }
    public List<string> QuestionTypes { get; set; } = new();
    public List<QuestionDto> Questions { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class QuestionDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "MultipleChoice";
    public string QuestionText { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public string? CorrectAnswer { get; set; }
    public string? Explanation { get; set; }
    public int Points { get; set; } = 1;
    public int OrderIndex { get; set; }
}

public class QuizAttemptDto
{
    public Guid Id { get; set; }
    public Guid QuizId { get; set; }
    public Guid UserId { get; set; }
    public int Score { get; set; }
    public int TotalPoints { get; set; }
    public decimal Percentage { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class SubmitQuizAttemptRequest
{
    public List<SubmitQuizAnswerDto> Answers { get; set; } = new();
}

public class SubmitQuizAnswerDto
{
    public Guid QuestionId { get; set; }
    public string UserAnswer { get; set; } = string.Empty;
}

public class QuizResultDto
{
    public Guid AttemptId { get; set; }
    public Guid QuizId { get; set; }
    public int Score { get; set; }
    public int TotalPoints { get; set; }
    public decimal Percentage { get; set; }
    public List<QuizResultAnswerDto> Answers { get; set; } = new();
    public DateTime CompletedAt { get; set; }
}

public class QuizResultAnswerDto
{
    public Guid QuestionId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public string UserAnswer { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public bool IsCorrect { get; set; }
    public int PointsAwarded { get; set; }
    public int Points { get; set; }
}
