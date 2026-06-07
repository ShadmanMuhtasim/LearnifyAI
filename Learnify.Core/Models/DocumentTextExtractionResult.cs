namespace Learnify.Core.Models;

public sealed record DocumentTextExtractionResult(
    string Text,
    string? Warning = null);
