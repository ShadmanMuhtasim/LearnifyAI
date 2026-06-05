using Learnify.Core.Models;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Learnify.Infrastructure.AI;

public sealed class AiQuizFormatException : InvalidOperationException
{
    public AiQuizFormatException(string reason, string outputPreview, int responseLength, Exception? innerException = null)
        : base("The local model returned an invalid quiz format. Try fewer questions or switch provider.", innerException)
    {
        Reason = reason;
        OutputPreview = outputPreview;
        ResponseLength = responseLength;
    }

    public string Reason { get; }
    public string OutputPreview { get; }
    public int ResponseLength { get; }
}

public static class QuizResponseParser
{
    private static readonly Regex ThinkBlockPattern = new(
        @"<think\b[^>]*>.*?</think>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly string[] QuestionTextKeys = ["questionText", "question_text", "question"];
    private static readonly string[] CorrectAnswerKeys = ["correctAnswer", "correct_answer", "answer"];
    private static readonly string[] OptionsKeys = ["options", "choices"];

    public static GeneratedQuizResult Parse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            throw CreateFailure("Provider returned an empty quiz response.", response);
        }

        var json = ExtractJson(response);
        try
        {
            using var document = JsonDocument.Parse(json);
            return ParseDocument(document.RootElement, response);
        }
        catch (AiQuizFormatException)
        {
            throw;
        }
        catch (JsonException ex)
        {
            throw CreateFailure("No valid balanced JSON object or array could be parsed.", response, ex);
        }
    }

    public static string ExtractJson(string response)
    {
        var clean = StripMarkdownFences(ThinkBlockPattern.Replace(response, " ")).Trim();
        var candidates = FindBalancedJsonCandidates(clean);

        foreach (var candidate in candidates.OrderByDescending(candidate => candidate.Length))
        {
            try
            {
                using var _ = JsonDocument.Parse(candidate);
                return candidate;
            }
            catch (JsonException)
            {
                // Try the next balanced candidate.
            }
        }

        throw CreateFailure("No valid balanced JSON object or array was found.", response);
    }

    private static GeneratedQuizResult ParseDocument(JsonElement root, string originalResponse)
    {
        JsonElement questionsElement;
        var result = new GeneratedQuizResult();

        if (root.ValueKind == JsonValueKind.Array)
        {
            questionsElement = root;
        }
        else if (root.ValueKind == JsonValueKind.Object)
        {
            result.Title = TryGetString(root, "title") ?? "Generated Quiz";
            if (!TryGetProperty(root, "questions", out questionsElement) ||
                questionsElement.ValueKind != JsonValueKind.Array)
            {
                throw CreateFailure("Quiz JSON did not include a questions array.", originalResponse);
            }
        }
        else
        {
            throw CreateFailure("Quiz JSON root must be an object or array.", originalResponse);
        }

        if (questionsElement.GetArrayLength() == 0)
        {
            throw CreateFailure("Quiz JSON contained an empty questions array.", originalResponse);
        }

        foreach (var questionElement in questionsElement.EnumerateArray())
        {
            if (questionElement.ValueKind != JsonValueKind.Object)
            {
                throw CreateFailure("Every quiz question must be a JSON object.", originalResponse);
            }

            var type = NormalizeQuestionType(TryGetString(questionElement, "type") ?? "MultipleChoice");
            var questionText = TryGetString(questionElement, QuestionTextKeys) ?? string.Empty;
            var correctAnswer = TryGetString(questionElement, CorrectAnswerKeys) ?? string.Empty;
            var explanation = TryGetString(questionElement, "explanation") ?? string.Empty;
            var options = ReadOptions(questionElement);

            if (string.IsNullOrWhiteSpace(questionText))
            {
                throw CreateFailure("A quiz question was missing question text.", originalResponse);
            }

            if (string.IsNullOrWhiteSpace(correctAnswer))
            {
                throw CreateFailure("A quiz question was missing a correct answer.", originalResponse);
            }

            if (type == "TrueFalse")
            {
                options = new List<string> { "True", "False" };
                correctAnswer = correctAnswer.Equals("false", StringComparison.OrdinalIgnoreCase)
                    ? "False"
                    : "True";
            }
            else if (type == "MultipleChoice")
            {
                if (options.Count < 2)
                {
                    throw CreateFailure("A multiple-choice question had fewer than 2 options.", originalResponse);
                }

                if (!options.Any(option => option.Equals(correctAnswer, StringComparison.OrdinalIgnoreCase)))
                {
                    throw CreateFailure("A multiple-choice correct answer did not match one of the options.", originalResponse);
                }
            }
            else
            {
                options = new List<string>();
            }

            result.Questions.Add(new GeneratedQuizQuestionResult
            {
                Type = type,
                QuestionText = questionText,
                Options = options,
                CorrectAnswer = correctAnswer,
                Explanation = explanation
            });
        }

        if (result.Questions.Count == 0)
        {
            throw CreateFailure("Quiz JSON contained no valid questions.", originalResponse);
        }

        return result;
    }

    private static string StripMarkdownFences(string value)
    {
        var clean = value.Trim();
        if (clean.StartsWith("```", StringComparison.Ordinal))
        {
            clean = clean[3..].TrimStart();
            if (clean.StartsWith("json", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean[4..].TrimStart();
            }
        }

        return clean.Replace("```json", "", StringComparison.OrdinalIgnoreCase)
            .Replace("```", "", StringComparison.Ordinal)
            .Trim();
    }

    private static List<string> FindBalancedJsonCandidates(string value)
    {
        var candidates = new List<string>();
        for (var start = 0; start < value.Length; start++)
        {
            if (value[start] is not ('{' or '['))
            {
                continue;
            }

            var end = FindBalancedEnd(value, start);
            if (end > start)
            {
                candidates.Add(value[start..(end + 1)]);
            }
        }

        return candidates;
    }

    private static int FindBalancedEnd(string value, int start)
    {
        var stack = new Stack<char>();
        var inString = false;
        var escaping = false;

        for (var index = start; index < value.Length; index++)
        {
            var current = value[index];
            if (inString)
            {
                if (escaping)
                {
                    escaping = false;
                }
                else if (current == '\\')
                {
                    escaping = true;
                }
                else if (current == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (current == '"')
            {
                inString = true;
                continue;
            }

            if (current is '{' or '[')
            {
                stack.Push(current);
                continue;
            }

            if (current is '}' or ']')
            {
                if (stack.Count == 0)
                {
                    return -1;
                }

                var opener = stack.Pop();
                if ((opener == '{' && current != '}') ||
                    (opener == '[' && current != ']'))
                {
                    return -1;
                }

                if (stack.Count == 0)
                {
                    return index;
                }
            }
        }

        return -1;
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string? TryGetString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGetProperty(element, name, out var value))
            {
                return value.ValueKind switch
                {
                    JsonValueKind.String => value.GetString()?.Trim(),
                    JsonValueKind.Number => value.GetRawText(),
                    JsonValueKind.True => "True",
                    JsonValueKind.False => "False",
                    _ => null
                };
            }
        }

        return null;
    }

    private static List<string> ReadOptions(JsonElement question)
    {
        JsonElement optionsElement = default;
        var hasOptions = false;
        foreach (var key in OptionsKeys)
        {
            if (TryGetProperty(question, key, out optionsElement))
            {
                hasOptions = true;
                break;
            }
        }

        if (!hasOptions || optionsElement.ValueKind != JsonValueKind.Array)
        {
            return new List<string>();
        }

        return optionsElement.EnumerateArray()
            .Select(option => option.ValueKind == JsonValueKind.String ? option.GetString()?.Trim() ?? "" : option.GetRawText())
            .Where(option => !string.IsNullOrWhiteSpace(option))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeQuestionType(string type)
    {
        var normalized = type.Replace(" ", "", StringComparison.OrdinalIgnoreCase)
            .Replace("-", "", StringComparison.OrdinalIgnoreCase)
            .Replace("_", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        return normalized.ToLowerInvariant() switch
        {
            "truefalse" => "TrueFalse",
            "shortanswer" => "ShortAnswer",
            "fillintheblank" => "FillInTheBlank",
            "fillblank" => "FillInTheBlank",
            _ => "MultipleChoice"
        };
    }

    private static AiQuizFormatException CreateFailure(string reason, string? response, Exception? innerException = null) =>
        new(reason, BuildPreview(response ?? string.Empty), response?.Length ?? 0, innerException);

    private static string BuildPreview(string value)
    {
        var withoutThink = ThinkBlockPattern.Replace(value, " ");
        var builder = new StringBuilder();
        foreach (var current in withoutThink)
        {
            if (char.IsControl(current))
            {
                if (builder.Length == 0 || builder[^1] != ' ')
                {
                    builder.Append(' ');
                }

                continue;
            }

            builder.Append(current);
            if (builder.Length >= 240)
            {
                break;
            }
        }

        return builder.ToString().Trim();
    }
}
