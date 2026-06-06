namespace Learnify.Core.Models;

public sealed record DocumentTextExtractionResult(
    string FileName,
    string ContentType,
    string ExtractedText,
    int CharacterCount,
    string? Warning);
