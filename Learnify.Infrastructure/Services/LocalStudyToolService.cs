using System.Text.RegularExpressions;
using Learnify.Core.Interfaces;
using Learnify.Core.Models;

namespace Learnify.Infrastructure.Services;

public sealed class LocalStudyToolService : ILocalStudyToolService
{
    private static readonly Regex SentenceSplitter = new(@"(?<=[.!?])\s+", RegexOptions.Compiled);
    private static readonly Regex WordMatcher = new(@"[A-Za-z][A-Za-z0-9'-]{2,}", RegexOptions.Compiled);
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "about", "after", "also", "because", "before", "between", "could", "each", "from", "have", "into",
        "more", "most", "note", "notes", "only", "other", "should", "study", "than", "that", "their",
        "there", "these", "this", "through", "using", "when", "where", "which", "while", "with", "would"
    };

    public string Summarize(string content)
    {
        var sentences = GetSentences(content);
        if (sentences.Count == 0)
        {
            throw new InvalidOperationException("Free Local generation needs readable note text.");
        }

        var selected = sentences
            .OrderByDescending(ScoreSentence)
            .ThenBy(sentence => sentences.IndexOf(sentence))
            .Take(3)
            .OrderBy(sentence => sentences.IndexOf(sentence))
            .ToList();

        var summary = string.Join(" ", selected);
        return summary.Length <= 700 ? summary : $"{summary[..697].TrimEnd()}...";
    }

    public IReadOnlyList<FlashcardResult> GenerateFlashcards(string content, int count)
    {
        var requestedCount = Math.Clamp(count, 1, 12);
        var sentences = GetSentences(content)
            .Where(sentence => WordMatcher.Matches(sentence).Count >= 5)
            .Take(requestedCount * 3)
            .ToList();

        if (sentences.Count == 0)
        {
            throw new InvalidOperationException("Free Local flashcards need readable note text.");
        }

        var cards = new List<FlashcardResult>();
        foreach (var sentence in sentences)
        {
            var concept = PickConcept(sentence);
            var question = string.IsNullOrWhiteSpace(concept)
                ? "What is an important point from this note?"
                : $"What should you remember about {concept}?";
            cards.Add(new FlashcardResult(question, sentence));

            if (cards.Count == requestedCount)
            {
                break;
            }
        }

        return cards;
    }

    public string GenerateStudyTips(string content)
    {
        var sentences = GetSentences(content);
        if (sentences.Count == 0)
        {
            throw new InvalidOperationException("Free Local study tips need readable note text.");
        }

        var keyTerms = ExtractKeyTerms(content).Take(6).ToList();
        var focusItems = keyTerms.Count == 0
            ? "the main definitions, examples, and cause/effect relationships in the note"
            : string.Join(", ", keyTerms);

        return "## Study Tips\n" +
               "- Start with a quick reread, then close the note and explain the main idea from memory.\n" +
               $"- Build a self-test around: {focusItems}.\n" +
               "- Convert each heading or dense paragraph into one question and one answer.\n" +
               "- Mark any sentence you cannot explain in your own words and review it again after a short break.\n" +
               "- Finish by writing a three-bullet recap without looking at the source text.";
    }

    private static List<string> GetSentences(string content)
        => SentenceSplitter.Split(content.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal))
            .Select(sentence => sentence.Trim())
            .Where(sentence => sentence.Length >= 24)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static int ScoreSentence(string sentence)
        => WordMatcher.Matches(sentence)
            .Select(match => match.Value)
            .Count(word => !StopWords.Contains(word));

    private static string PickConcept(string sentence)
    {
        var terms = ExtractKeyTerms(sentence).Take(3).ToList();
        return terms.Count == 0 ? string.Empty : string.Join(" / ", terms);
    }

    private static IEnumerable<string> ExtractKeyTerms(string content)
        => WordMatcher.Matches(content)
            .Select(match => match.Value.Trim('\'', '-'))
            .Where(word => word.Length >= 4 && !StopWords.Contains(word))
            .GroupBy(word => word.ToLowerInvariant())
            .OrderByDescending(group => group.Count())
            .ThenByDescending(group => group.Key.Length)
            .Select(group => group.First());
}
