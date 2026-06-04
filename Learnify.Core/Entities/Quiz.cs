namespace Learnify.Core.Entities;

public class Quiz : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? CourseId { get; set; }
    public Guid? NoteId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Difficulty { get; set; } = "Medium";
    public string QuestionTypes { get; set; } = "MultipleChoice";
    public int? TimeLimitMinutes { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual Course? Course { get; set; }
    public virtual Note? Note { get; set; }
    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
    public virtual ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
}

public class Question : BaseEntity
{
    public Guid QuizId { get; set; }
    public string Type { get; set; } = "MultipleChoice";
    public string QuestionText { get; set; } = string.Empty;
    public string? OptionsJson { get; set; }
    public string CorrectAnswer { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public int Points { get; set; } = 1;
    public int OrderIndex { get; set; }

    public virtual Quiz Quiz { get; set; } = null!;
}

public class QuizAttempt : BaseEntity
{
    public Guid QuizId { get; set; }
    public Guid UserId { get; set; }
    public int Score { get; set; }
    public int TotalPoints { get; set; }
    public decimal Percentage { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public virtual Quiz Quiz { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual ICollection<QuizAttemptAnswer> Answers { get; set; } = new List<QuizAttemptAnswer>();
}

public class QuizAttemptAnswer : BaseEntity
{
    public Guid QuizAttemptId { get; set; }
    public Guid QuestionId { get; set; }
    public string UserAnswer { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int PointsAwarded { get; set; }

    public virtual QuizAttempt QuizAttempt { get; set; } = null!;
    public virtual Question Question { get; set; } = null!;
}
