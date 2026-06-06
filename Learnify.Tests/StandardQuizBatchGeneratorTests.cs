using Learnify.Core.Interfaces;
using Learnify.Core.Models;
using Learnify.Infrastructure.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Learnify.Tests;

public class StandardQuizBatchGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_SplitsSevenQuestionRequestIntoFiveAndTwo()
    {
        var provider = new QueueProvider(
            BuildQuizJson(1, 5),
            BuildQuizJson(6, 2));

        var result = await StandardQuizBatchGenerator.GenerateAsync(
            provider,
            "Gemini",
            "gemini-3.5-flash",
            StudyContent,
            QuestionTypes,
            "Medium",
            7,
            NullLogger.Instance,
            CancellationToken.None);

        Assert.Equal(7, result.Questions.Count);
        Assert.Equal(2, provider.Prompts.Count);
        Assert.Contains("exactly 5", provider.Prompts[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("exactly 2", provider.Prompts[1], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateAsync_ThrowsClearFailureWhenProviderCannotFillRequestedCount()
    {
        var provider = new QueueProvider(
            BuildQuizJson(1, 2),
            BuildQuizJson(3, 1),
            BuildQuizJson(4, 1));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            StandardQuizBatchGenerator.GenerateAsync(
                provider,
                "Gemini",
                "gemini-3.5-flash",
                StudyContent,
                QuestionTypes,
                "Medium",
                5,
                NullLogger.Instance,
                CancellationToken.None));

        Assert.Contains("AI provider could not generate the requested quiz size", ex.Message);
        Assert.True(provider.Prompts.Count >= 3);
    }

    private static readonly IReadOnlyList<string> QuestionTypes =
        ["MultipleChoice", "TrueFalse", "ShortAnswer", "FillInTheBlank"];

    private const string StudyContent =
        "Hash maps use hash functions, buckets, separate chaining, open addressing, load factor, resizing, and average O(1) operations.";

    private static string BuildQuizJson(int start, int count)
    {
        var questions = Enumerable.Range(start, count).Select(index => $$"""
        {
          "type": "MultipleChoice",
          "questionText": "Question {{index}} about hash maps?",
          "options": ["Answer {{index}}", "Distractor A", "Distractor B", "Distractor C"],
          "correctAnswer": "Answer {{index}}",
          "explanation": "Explanation {{index}}."
        }
        """);

        return $$"""
        {
          "title": "Batch Quiz",
          "questions": [
            {{string.Join(",", questions)}}
          ]
        }
        """;
    }

    private sealed class QueueProvider : IAiProvider
    {
        private readonly Queue<string> _responses;

        public QueueProvider(params string[] responses)
        {
            _responses = new Queue<string>(responses);
        }

        public string ProviderName => "Gemini";
        public List<string> Prompts { get; } = new();

        public Task<string> CompleteAsync(string prompt, AiRequestOptions options, CancellationToken ct = default)
        {
            Prompts.Add(prompt);
            return Task.FromResult(_responses.Dequeue());
        }
    }
}
