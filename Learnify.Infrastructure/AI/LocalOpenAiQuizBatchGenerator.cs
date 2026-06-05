using Learnify.Core.Interfaces;
using Learnify.Core.Models;
using Microsoft.Extensions.Logging;

namespace Learnify.Infrastructure.AI;

public static class LocalOpenAiQuizBatchGenerator
{
    private const int BatchSize = 4;
    private const int RetryBatchSize = 2;
    private const int MaxExtraBatches = 2;
    private const int InitialContentLimit = 6500;
    private const int RetryContentLimit = 4500;
    private const int SingleQuestionContentLimit = 2400;

    public static async Task<GeneratedQuizResult> GenerateAsync(
        IAiProvider provider,
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
                var batch = await GenerateBatchWithRetryAsync(
                    provider,
                    model,
                    content,
                    normalizedTypes,
                    normalizedDifficulty,
                    batchCount,
                    batchNumber,
                    seen,
                    logger,
                    ct);

                AddUniqueQuestions(merged, batch, seen, requestedCount);
            }
            catch (Exception ex) when (IsRetryableLocalQuizFailure(ex))
            {
                logger.LogWarning(
                    ex,
                    "LocalOpenAI quiz batch did not produce usable questions. Model={Model}, Batch={Batch}, Requested={Requested}",
                    model,
                    batchNumber,
                    batchCount);
            }

            batchNumber++;
        }

        if (merged.Questions.Count < requestedCount)
        {
            await FillWithSingleQuestionFallbackAsync(
                provider,
                model,
                content,
                normalizedTypes,
                normalizedDifficulty,
                requestedCount,
                merged,
                seen,
                logger,
                ct);
        }

        if (merged.Questions.Count < requestedCount)
        {
            throw CreateRequestedSizeFailure();
        }

        return merged;
    }

    private static async Task<GeneratedQuizResult> GenerateBatchWithRetryAsync(
        IAiProvider provider,
        string model,
        string content,
        IReadOnlyList<string> normalizedTypes,
        string normalizedDifficulty,
        int batchCount,
        int batchNumber,
        IReadOnlySet<string> existingQuestions,
        ILogger logger,
        CancellationToken ct)
    {
        try
        {
            return await GenerateBatchAsync(
                provider,
                content,
                normalizedTypes,
                normalizedDifficulty,
                batchCount,
                batchNumber,
                existingQuestions,
                InitialContentLimit,
                maxTokens: 2600,
                ct);
        }
        catch (Exception ex) when (IsRetryableLocalQuizFailure(ex))
        {
            var retryCount = Math.Min(RetryBatchSize, batchCount);
            logger.LogWarning(
                ex,
                "LocalOpenAI quiz batch failed; retrying smaller batch. Model={Model}, Batch={Batch}, Requested={Requested}, RetryCount={RetryCount}",
                model,
                batchNumber,
                batchCount,
                retryCount);

            try
            {
                return await GenerateBatchAsync(
                    provider,
                    content,
                    normalizedTypes,
                    normalizedDifficulty,
                    retryCount,
                    batchNumber,
                    existingQuestions,
                    RetryContentLimit,
                    maxTokens: 2400,
                    ct);
            }
            catch (Exception retryEx) when (IsRetryableLocalQuizFailure(retryEx))
            {
                throw CreateRequestedSizeFailure(retryEx);
            }
        }
    }

    private static async Task<GeneratedQuizResult> GenerateBatchAsync(
        IAiProvider provider,
        string content,
        IReadOnlyList<string> normalizedTypes,
        string normalizedDifficulty,
        int batchCount,
        int batchNumber,
        IReadOnlySet<string> existingQuestions,
        int contentLimit,
        int maxTokens,
        CancellationToken ct)
    {
        var prompt = BuildPrompt(
            content,
            normalizedTypes,
            normalizedDifficulty,
            batchCount,
            batchNumber,
            existingQuestions,
            contentLimit);
        var response = await provider.CompleteAsync(
            prompt,
            new AiRequestOptions { MaxTokens = maxTokens, Temperature = 0.1f },
            ct);

        return QuizResponseParser.Parse(response);
    }

    private static async Task FillWithSingleQuestionFallbackAsync(
        IAiProvider provider,
        string model,
        string content,
        IReadOnlyList<string> normalizedTypes,
        string normalizedDifficulty,
        int requestedCount,
        GeneratedQuizResult merged,
        HashSet<string> seen,
        ILogger logger,
        CancellationToken ct)
    {
        var maxAttempts = Math.Max(requestedCount * 3, requestedCount + 4);
        var attempt = 1;
        while (merged.Questions.Count < requestedCount && attempt <= maxAttempts)
        {
            try
            {
                var response = await provider.CompleteAsync(
                    BuildSingleQuestionPrompt(
                        content,
                        normalizedTypes,
                        normalizedDifficulty,
                        seen,
                        attempt),
                    new AiRequestOptions { MaxTokens = 900, Temperature = 0.1f },
                    ct);
                var single = QuizResponseParser.Parse(response);
                AddUniqueQuestions(merged, single, seen, requestedCount);
            }
            catch (Exception ex) when (IsRetryableLocalQuizFailure(ex))
            {
                logger.LogWarning(
                    ex,
                    "LocalOpenAI one-question fallback attempt failed. Model={Model}, Attempt={Attempt}, Collected={Collected}, Requested={Requested}",
                    model,
                    attempt,
                    merged.Questions.Count,
                    requestedCount);
            }

            attempt++;
        }
    }

    private static string BuildPrompt(
        string content,
        IReadOnlyList<string> normalizedTypes,
        string normalizedDifficulty,
        int batchCount,
        int batchNumber,
        IReadOnlySet<string> existingQuestions,
        int contentLimit)
    {
        var snippet = content[..Math.Min(content.Length, contentLimit)];
        var avoid = existingQuestions.Count == 0
            ? ""
            : "\nAvoid repeating these question texts:\n" + string.Join("\n", existingQuestions.Take(8).Select(question => $"- {question}"));

        return
            "/no_think\n" +
            "Return final JSON only. Do not include reasoning, markdown, commentary, XML tags, or code fences.\n" +
            "Use double quotes only. No trailing text.\n" +
            "Schema: {\"title\":\"string\",\"questions\":[{\"type\":\"MultipleChoice\",\"questionText\":\"string\",\"options\":[\"A\",\"B\",\"C\",\"D\"],\"correctAnswer\":\"A\",\"explanation\":\"string\"}]}\n" +
            $"Generate exactly {batchCount} distinct questions for batch {batchNumber}. Difficulty: {normalizedDifficulty}. Allowed types: {string.Join(", ", normalizedTypes)}.\n" +
            "MultipleChoice: 4 options; correctAnswer exactly matches an option. TrueFalse: options [\"True\",\"False\"]. ShortAnswer and FillInTheBlank: options []. FillInTheBlank uses ____.\n" +
            $"{avoid}\n" +
            "Study material:\n" +
            snippet;
    }

    private static string BuildSingleQuestionPrompt(
        string content,
        IReadOnlyList<string> normalizedTypes,
        string normalizedDifficulty,
        IReadOnlySet<string> existingQuestions,
        int attempt)
    {
        var snippet = GetRotatingExcerpt(content, SingleQuestionContentLimit, attempt);
        var avoid = existingQuestions.Count == 0
            ? ""
            : "\nDo not duplicate these question texts:\n" + string.Join("\n", existingQuestions.TakeLast(8).Select(question => $"- {question}"));

        return
            "/no_think\n" +
            "ONE quiz question only. Return ONLY valid JSON. No markdown. No commentary. No explanation outside JSON. No <think> tags. No code fences.\n" +
            "Schema: {\"questions\":[{\"question\":\"string\",\"type\":\"multiple_choice\",\"options\":[\"A\",\"B\",\"C\",\"D\"],\"correctAnswer\":\"A\",\"explanation\":\"string\"}]}\n" +
            $"Difficulty: {normalizedDifficulty}. Allowed types: {string.Join(", ", normalizedTypes)}. If multiple_choice, correctAnswer must exactly match an option.\n" +
            $"{avoid}\n" +
            "Study material excerpt:\n" +
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

    private static string GetRotatingExcerpt(string content, int limit, int attempt)
    {
        if (content.Length <= limit)
        {
            return content;
        }

        var maxStart = Math.Max(0, content.Length - limit);
        var offset = ((attempt - 1) * (limit / 2)) % (maxStart + 1);
        return content.Substring(offset, Math.Min(limit, content.Length - offset));
    }

    private static InvalidOperationException CreateRequestedSizeFailure(Exception? innerException = null) =>
        new("The local model could not generate the requested quiz size. Try fewer questions or switch provider.", innerException);

    private static bool IsRetryableLocalQuizFailure(Exception ex) =>
        ex is AiQuizFormatException ||
        ex.Message.Contains("could not generate the requested quiz size", StringComparison.OrdinalIgnoreCase) ||
        ex.Message.Contains("choices[0].message.content", StringComparison.OrdinalIgnoreCase) ||
        ex.Message.Contains("LocalOpenAI response did not include", StringComparison.OrdinalIgnoreCase) ||
        ex.Message.Contains("finish_reason=length", StringComparison.OrdinalIgnoreCase);
}
