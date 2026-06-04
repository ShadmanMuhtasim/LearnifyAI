using Learnify.Core.Interfaces;
using Learnify.Core.Models;

namespace Learnify.Infrastructure.AI.Providers;

/// <summary>
/// Demo provider — no API key required. Returns realistic static responses.
/// Used as the safe fallback when no provider is configured.
/// </summary>
public class MockAiProvider : IAiProvider
{
    public string ProviderName => "Mock";

    public Task<string> CompleteAsync(string prompt, AiRequestOptions options, CancellationToken ct = default)
    {
        if (prompt.Contains("JSON array") || prompt.Contains("flashcard") || prompt.Contains("term"))
        {
            return Task.FromResult("""
            [
              {"question": "What is Clean Architecture?", "answer": "It separates code into Core, Application, Infrastructure, and Presentation layers."},
              {"question": "What is the Repository Pattern?", "answer": "It abstracts data access logic behind an interface, keeping business logic clean."},
              {"question": "What is Dependency Injection?", "answer": "Objects receive their dependencies from outside rather than creating them internally."},
              {"question": "What is a JWT token?", "answer": "It is a signed token that carries user identity claims and can be validated without hitting the database."},
              {"question": "What is Entity Framework Core?", "answer": "It is an ORM that maps C# classes to database tables and handles queries via LINQ."}
            ]
            """.Trim());
        }

        if (prompt.Contains("Summarize") || prompt.Contains("summarize"))
        {
            return Task.FromResult(
                "This is a mock summary. The content covers key educational concepts with practical examples. " +
                "Configure a real AI provider (Gemini, OpenAI, Claude) in AI Settings for actual summarization.");
        }

        if (prompt.Contains("study tip") || prompt.Contains("Study tip"))
        {
            return Task.FromResult(
                "1. Use active recall — retrieve concepts from memory before checking notes.\n" +
                "2. Apply spaced repetition — review at 1 day, 3 days, then 1 week intervals.\n" +
                "3. Teach the concept — explain it simply as if teaching a beginner.\n" +
                "4. Break topics into small chunks — master one before moving to the next.\n" +
                "5. Configure a real AI provider in Settings for personalized tips.");
        }

        return Task.FromResult("Mock AI response. Configure a real AI provider in Settings.");
    }
}
