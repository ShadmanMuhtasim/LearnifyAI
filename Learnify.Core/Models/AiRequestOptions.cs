namespace Learnify.Core.Models;

/// <summary>
/// Options for AI request parameters.
/// </summary>
public record AiRequestOptions(
    double Temperature = 0.4,
    int MaxTokens = 1024);