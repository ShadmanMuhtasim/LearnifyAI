using Learnify.Core.Interfaces;
using Learnify.Core.Models;
using Learnify.Infrastructure.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Learnify.Tests;

public class LocalOpenAiQuizBatchGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_CombinesTwoFourQuestionBatchesIntoEightQuestions()
    {
        var provider = new QueueProvider(
            BuildQuizJson(1, 4),
            BuildQuizJson(5, 4));

        var result = await LocalOpenAiQuizBatchGenerator.GenerateAsync(
            provider,
            "Qwen3.6-35B-A3B-UD-Q4_K_M.gguf",
            StudyContent,
            QuestionTypes,
            "Medium",
            8,
            NullLogger.Instance,
            CancellationToken.None);

        Assert.Equal(8, result.Questions.Count);
        Assert.Equal(2, provider.Prompts.Count);
        Assert.All(provider.Prompts, prompt => Assert.Contains("/no_think", prompt));
    }

    [Fact]
    public async Task GenerateAsync_RetriesEmptyLengthBatchWithSmallerBatchAndCompletes()
    {
        var provider = new QueueProvider(
            new InvalidOperationException("LocalOpenAI response did not include non-empty choices[0].message.content."),
            BuildQuizJson(1, 2),
            BuildQuizJson(3, 4),
            BuildQuizJson(7, 2));

        var result = await LocalOpenAiQuizBatchGenerator.GenerateAsync(
            provider,
            "Qwen3.6-35B-A3B-UD-Q4_K_M.gguf",
            StudyContent,
            QuestionTypes,
            "Medium",
            8,
            NullLogger.Instance,
            CancellationToken.None);

        Assert.Equal(8, result.Questions.Count);
        Assert.Equal(4, provider.Prompts.Count);
        Assert.Contains(provider.Options, options => options.MaxTokens == 2400);
    }

    [Fact]
    public async Task GenerateAsync_InvalidBatchAfterRetryReturnsClearFailure()
    {
        var responses = new List<object>
        {
            """{"questions":[{"type":"MultipleChoice","questionText":"Bad","options":["Only one"],"correctAnswer":"Only one"}]}""",
            """{"questions":[{"type":"MultipleChoice","questionText":"Still bad","options":["Only one"],"correctAnswer":"Only one"}]}"""
        };
        responses.AddRange(Enumerable.Repeat<object>(
                """{"questions":[{"type":"MultipleChoice","questionText":"Still bad single","options":["Only one"],"correctAnswer":"Only one"}]}""",
                24));
        var provider = new QueueProvider(responses.ToArray());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            LocalOpenAiQuizBatchGenerator.GenerateAsync(
                provider,
                "Qwen3.6-35B-A3B-UD-Q4_K_M.gguf",
                StudyContent,
                QuestionTypes,
                "Medium",
                4,
                NullLogger.Instance,
                CancellationToken.None));

        Assert.Contains("could not generate the requested quiz size", ex.Message);
        Assert.True(provider.Prompts.Count >= 14);
    }

    [Fact]
    public async Task GenerateAsync_FallsBackToSingleQuestionModeForSevenQuestions()
    {
        var responses = new List<object>
        {
            InvalidLength(),
            InvalidLength()
        };
        responses.AddRange(Enumerable.Range(1, 7).Select(index => BuildQuizJson(index, 1)));
        var provider = new QueueProvider(responses.ToArray());

        var result = await LocalOpenAiQuizBatchGenerator.GenerateAsync(
            provider,
            "Qwen3.6-35B-A3B-UD-Q4_K_M.gguf",
            StudyContent,
            QuestionTypes,
            "Medium",
            7,
            NullLogger.Instance,
            CancellationToken.None);

        Assert.Equal(7, result.Questions.Count);
        Assert.True(provider.Prompts.Count >= 9);
        Assert.Contains(provider.Prompts, prompt => prompt.Contains("ONE quiz question only", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GenerateAsync_FallsBackToSingleQuestionModeForEightQuestions()
    {
        var responses = new List<object>
        {
            InvalidLength(),
            InvalidLength(),
            InvalidLength(),
            InvalidLength()
        };
        responses.AddRange(Enumerable.Range(1, 8).Select(index => BuildQuizJson(index, 1)));
        var provider = new QueueProvider(responses.ToArray());

        var result = await LocalOpenAiQuizBatchGenerator.GenerateAsync(
            provider,
            "Qwen3.6-35B-A3B-UD-Q4_K_M.gguf",
            StudyContent,
            QuestionTypes,
            "Medium",
            8,
            NullLogger.Instance,
            CancellationToken.None);

        Assert.Equal(8, result.Questions.Count);
        Assert.Contains(provider.Options, options => options.MaxTokens == 900);
    }

    [Fact]
    public async Task GenerateAsync_SkipsDuplicateSingleQuestionAndKeepsAttempting()
    {
        var responses = new List<object>
        {
            InvalidLength(),
            InvalidLength(),
            BuildQuizJson(1, 1),
            BuildQuizJson(1, 1),
            BuildQuizJson(2, 1),
            BuildQuizJson(3, 1),
            BuildQuizJson(4, 1)
        };
        var provider = new QueueProvider(responses.ToArray());

        var result = await LocalOpenAiQuizBatchGenerator.GenerateAsync(
            provider,
            "Qwen3.6-35B-A3B-UD-Q4_K_M.gguf",
            StudyContent,
            QuestionTypes,
            "Medium",
            4,
            NullLogger.Instance,
            CancellationToken.None);

        Assert.Equal(4, result.Questions.Count);
        Assert.Equal(4, result.Questions.Select(question => question.QuestionText).Distinct().Count());
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

    private static InvalidOperationException InvalidLength() =>
        new("LocalOpenAI response did not include non-empty choices[0].message.content.");

    private sealed class QueueProvider : IAiProvider
    {
        private readonly Queue<object> _responses;

        public QueueProvider(params object[] responses)
        {
            _responses = new Queue<object>(responses);
        }

        public string ProviderName => "LocalOpenAI";
        public List<string> Prompts { get; } = new();
        public List<AiRequestOptions> Options { get; } = new();

        public Task<string> CompleteAsync(string prompt, AiRequestOptions options, CancellationToken ct = default)
        {
            Prompts.Add(prompt);
            Options.Add(options);

            var next = _responses.Dequeue();
            if (next is Exception ex)
            {
                throw ex;
            }

            return Task.FromResult((string)next);
        }
    }
}
