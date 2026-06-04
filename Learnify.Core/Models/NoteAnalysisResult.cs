namespace Learnify.Core.Models;

/// <summary>
/// Structured analysis output returned by the AI document workflow.
/// </summary>
public class NoteAnalysisResult
{
    public string SuggestedCourseName { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<string> DetectedTopics { get; set; } = new();
    public string ExtractedText { get; set; } = string.Empty;
}
