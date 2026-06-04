namespace Learnify.Core.Models;

public class GeneratedQuizResult
{
    public string Title { get; set; } = "Generated Quiz";
    public List<GeneratedQuizQuestionResult> Questions { get; set; } = new();
}

public class GeneratedQuizQuestionResult
{
    public string Type { get; set; } = "MultipleChoice";
    public string QuestionText { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public string CorrectAnswer { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
}
