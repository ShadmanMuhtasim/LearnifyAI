namespace Learnify.Core.Models;

public sealed record DocumentTextExtractionResult(
    string Text,
    string Status,
    string? Warning = null);
