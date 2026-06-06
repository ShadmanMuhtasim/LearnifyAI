using Learnify.Core.Interfaces;
using Learnify.Core.Models;
using Microsoft.Extensions.Logging;

namespace Learnify.Infrastructure.AI;

public static class StandardQuizBatchGenerator
{
    private const int BatchSize = 5;
    private const int MaxExtraBatches = 2;
    private const int ContentLimit = 9000;

    public static async Task<GeneratedQuizResult> GenerateAsync(
        IAiProvider provider,
        string providerName,
        string model,
        string content,
        IReadOnlyList<string> normalizedTypes,
        string normalizedDifficulty,
        int requestedCount,
        ILogger logger,
        CancellationToken ct = default)
    {
        var merged = new GeneratedQuizResult { Title = "Generated Quiz" };
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var batchNumber = 1;
        var maxBatches = (int)Math.Ceiling(requestedCount / (double)BatchSize) + MaxExtraBatches;

        while (merged.Questions.Count < requestedCount && batchNumber <= maxBatches)
        {
            var remaining = requestedCount - merged.Questions.Count;
            var batchCount = Math.Min(BatchSize, remaining);

            try
            {
                var response = await provider.CompleteAsync(
                    BuildPrompt(content, normalizedTypes, normalizedDifficulty, batchCount, batchNumber, seen),
                    new AiRequestOptions { MaxTokens = 3200, Temperature = 0.1f },
                    ct);
                var batch = QuizResponseParser.Parse(response);
                AddUniqueQuestions(merged, batch, seen, requestedCount);
            }
            catch (Exception ex) when (IsRetryableProviderQuizFailure(ex))
            {
                logger.LogWarning(
                    ex,
                    "AI provider quiz batch failed. Provider={Provider}, Model={Model}, Batch={Batch}, Requested={Requested}, Collected={Collected}",
                    providerName,
                    model,
                    batchNumber,
                    batchCount,
                    merged.Questions.Count);
            }

            batchNumber++;
        }

        if (merged.Questions.Count < requestedCount)
        {
            throw new InvalidOperationException("The AI provider could not generate the requested quiz size. Try fewer questions or switch provider.");
        }

        return merged;
    }

    private static string BuildPrompt(
        string content,
        IReadOnlyList<string> normalizedTypes,
        string normalizedDifficulty,
        int batchCount,
        int batchNumber,
        IReadOnlySet<string> existingQuestions)
    {
        var snippet = content[..Math.Min(content.Length, ContentLimit)];
        var avoid = existingQuestions.Count == 0
            ? ""
            : "\nAvoid repeating these question texts:\n" + string.Join("\n", existingQuestions.Take(10).Select(question => $"- {question}"));

        return
            "Create a quiz batch from the study material.\n" +
            "Return ONLY valid JSON. No markdown. No code fences. No explanation. No commentary. No trailing text.\n" +
            "Use double quotes for all JSON strings and property names.\n" +
            "Schema:\n" +
            "{\"title\":\"string\",\"questions\":[{\"type\":\"MultipleChoice\",\"questionText\":\"string\",\"options\":[\"option 1\",\"option 2\",\"option 3\",\"option 4\"],\"correctAnswer\":\"option 1\",\"explanation\":\"string\"}]}\n" +
            $"Rules: exactly {batchCount} distinct questions for batch {batchNumber}; difficulty {normalizedDifficulty}; allowed types: {string.Join(", ", normalizedTypes)}.\n" +
            "MultipleChoice: include 4 concise options; correctAnswer must exactly match one option.\n" +
            "TrueFalse: options must be [\"True\",\"False\"]; correctAnswer must be \"True\" or \"False\".\n" +
            "ShortAnswer: options must be []; correctAnswer must be concise.\n" +
            "FillInTheBlank: questionText must contain ____; options must be []; correctAnswer must be concise.\n" +
            $"{avoid}\n" +
            "Study material:\n" +
            snippet;
    }

    private static void AddUniqueQuestions(
        GeneratedQuizResult merged,
        GeneratedQuizResult batch,
        HashSet<string> seen,
        int requestedCount)
    {
        foreach (var question in batch.Questions)
        {
            if (merged.Questions.Count >= requestedCount)
            {
                return;
            }

            var key = NormalizeQuestionKey(question.QuestionText);
            if (string.IsNullOrWhiteSpace(key) || !seen.Add(key))
            {
                continue;
            }

            merged.Questions.Add(question);
        }
    }

    private static string NormalizeQuestionKey(string value) =>
        string.Join(" ", value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static bool IsRetryableProviderQuizFailure(Exception ex) =>
        ex is AiQuizFormatException ||
        ex.Message.Contains("empty", StringComparison.OrdinalIgnoreCase) ||
        ex.Message.Contains("invalid quiz", StringComparison.OrdinalIgnoreCase) ||
        ex.Message.Contains("requested quiz size", StringComparison.OrdinalIgnoreCase);
}
