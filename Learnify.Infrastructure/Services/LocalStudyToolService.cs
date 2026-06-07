using System.Text;
using System.Text.RegularExpressions;
using Learnify.Core.Interfaces;
using Learnify.Core.Models;

namespace Learnify.Infrastructure.Services;

public sealed class LocalStudyToolService : ILocalStudyToolService
{
    private const string LocalNotice = "Generated locally without AI. Quality may be simpler than AI-generated output.";

    private static readonly string[] AcademicKeywords =
    [
        "rule", "exception", "principle", "doctrine", "case", "section", "article",
        "definition", "classification", "cause", "effect", "formula", "example",
        "includes", "consists", "therefore", "because", "means", "defined"
    ];

    public string GenerateSummary(string content, string summaryDepth = "Balanced")
    {
        var normalized = Normalize(content);
        var sentences = SplitSentences(normalized);
        if (sentences.Count == 0)
        {
            return $"{LocalNotice}\n\n## Overview\n\nNo readable study content was provided.";
        }

        var depth = NormalizeDepth(summaryDepth);
        var maxKeyPoints = depth switch { "Quick" => 4, "Detailed" => 10, _ => 7 };
        var maxDetails = depth switch { "Quick" => 3, "Detailed" => 8, _ => 5 };
        var ranked = RankSentences(sentences).ToList();
        var terms = ExtractTerms(normalized, 8);

        var overview = ranked.Count > 0 ? ranked[0].Text : sentences[0];
        var keyPoints = ranked.Take(maxKeyPoints).Select(item => item.Text).ToList();
        var details = ranked
            .Where(item => HasImportantDetail(item.Text))
            .Take(maxDetails)
            .Select(item => item.Text)
            .DefaultIfEmpty(keyPoints.First())
            .ToList();

        var builder = new StringBuilder();
        builder.AppendLine(LocalNotice);
        builder.AppendLine();
        builder.AppendLine("## Overview");
        builder.AppendLine();
        builder.AppendLine(overview);
        builder.AppendLine();
        AppendBullets(builder, "## Key Points", keyPoints);
        AppendBullets(builder, "## Important Details", details);
        builder.AppendLine("## Important Terms");
        builder.AppendLine();
        foreach (var term in terms)
        {
            var meaning = FindSentenceForTerm(term, sentences);
            builder.AppendLine($"- {term}: {meaning}");
        }
        builder.AppendLine();
        AppendBullets(builder, "## Must Remember", ranked.Take(Math.Min(5, maxKeyPoints)).Select(item => item.Text));
        AppendBullets(builder, "## Short Revision Summary", keyPoints.Take(depth == "Detailed" ? 5 : 3));
        return builder.ToString().Trim();
    }

    public IReadOnlyList<FlashcardResult> GenerateFlashcards(string content, int maxCards = 10)
    {
        var normalized = Normalize(content);
        var cards = new List<FlashcardResult>();

        foreach (var (term, answer) in ExtractDefinitionCards(normalized))
        {
            AddCard(cards, $"What is {term}?", answer, maxCards);
        }

        foreach (var heading in ExtractHeadings(normalized))
        {
            var answer = FindFollowingText(normalized, heading);
            AddCard(cards, $"Explain {heading}.", answer, maxCards);
        }

        foreach (var sentence in RankSentences(SplitSentences(normalized)).Select(item => item.Text))
        {
            if (cards.Count >= maxCards)
            {
                break;
            }

            var question = sentence.Contains(" because ", StringComparison.OrdinalIgnoreCase) ||
                           sentence.Contains(" causes ", StringComparison.OrdinalIgnoreCase)
                ? "What cause/effect relationship is described here?"
                : sentence.Contains(" exception ", StringComparison.OrdinalIgnoreCase) ||
                  sentence.Contains(" rule ", StringComparison.OrdinalIgnoreCase)
                    ? "What rule or exception is described here?"
                    : $"What are the key points about {ExtractFocusTerm(sentence)}?";

            AddCard(cards, question, sentence, maxCards);
        }

        return cards.Count > 0
            ? cards
            : [new FlashcardResult("What is the main idea of this material?", normalized[..Math.Min(normalized.Length, 240)])];
    }

    public string GenerateStudyTips(string content)
    {
        var normalized = Normalize(content);
        var headings = ExtractHeadings(normalized).Take(6).ToList();
        var terms = ExtractTerms(normalized, 8);
        var ranked = RankSentences(SplitSentences(normalized)).Take(6).Select(item => item.Text).ToList();
        var concepts = terms.Count > 0 ? terms : ranked.Select(ExtractFocusTerm).Distinct().Take(6).ToList();
        var reviewOrder = headings.Count > 0 ? headings : concepts;

        var builder = new StringBuilder();
        builder.AppendLine(LocalNotice);
        builder.AppendLine();
        AppendBullets(builder, "## Active Recall Questions", concepts.Select(term => $"Explain {term} without looking at the notes."));
        AppendBullets(builder, "## Key Concepts to Master", concepts);
        AppendBullets(builder, "## Common Confusions", ranked.Select(item => $"Check the exact wording and conditions in: {item}"));
        AppendBullets(builder, "## Mini Study Plan", reviewOrder.Select((item, index) => $"{index + 1}. Review {item}, then close the note and recall the key facts."));
        AppendBullets(builder, "## Self-Test Questions", ranked.Select(item => $"What exam question could be answered by: {item}"));
        return builder.ToString().Trim();
    }

    private static string Normalize(string value) =>
        Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim();

    private static string NormalizeDepth(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "quick" => "Quick",
            "detailed" => "Detailed",
            _ => "Balanced"
        };

    private static IReadOnlyList<string> SplitSentences(string text) =>
        Regex.Split(text, @"(?<=[.!?])\s+|(?:\r?\n)+")
            .Select(item => item.Trim())
            .Where(item => item.Length >= 24)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static IEnumerable<(string Text, int Score)> RankSentences(IReadOnlyList<string> sentences)
    {
        var frequencies = sentences
            .SelectMany(sentence => Regex.Matches(sentence.ToLowerInvariant(), @"[a-z][a-z0-9]{3,}")
                .Select(match => match.Value))
            .GroupBy(word => word)
            .ToDictionary(group => group.Key, group => group.Count());

        return sentences
            .Select(sentence => (Text: sentence, Score: ScoreSentence(sentence, frequencies)))
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Text.Length);
    }

    private static int ScoreSentence(string sentence, IReadOnlyDictionary<string, int> frequencies)
    {
        var score = Regex.Matches(sentence.ToLowerInvariant(), @"[a-z][a-z0-9]{3,}")
            .Sum(match => frequencies.TryGetValue(match.Value, out var count) ? Math.Min(count, 4) : 0);

        if (HasImportantDetail(sentence)) score += 8;
        if (Regex.IsMatch(sentence, @"\b[A-Z][a-z]+(?:\s+[A-Z][a-z]+)+\b")) score += 4;
        if (sentence.Contains(':')) score += 2;
        return score;
    }

    private static bool HasImportantDetail(string value) =>
        Regex.IsMatch(value, @"\d|=|%|\b[A-Z][a-z]+(?:\s+[A-Z][a-z]+)+\b") ||
        AcademicKeywords.Any(keyword => value.Contains(keyword, StringComparison.OrdinalIgnoreCase));

    private static List<string> ExtractTerms(string text, int maxTerms)
    {
        var definitionTerms = ExtractDefinitionCards(text).Select(card => card.Term);
        var capitalizedTerms = Regex.Matches(text, @"\b[A-Z][A-Za-z0-9]*(?:\s+[A-Z][A-Za-z0-9]*){0,3}\b")
            .Select(match => match.Value.Trim())
            .Where(term => term.Length > 3 && !term.StartsWith("The ", StringComparison.OrdinalIgnoreCase));

        return definitionTerms
            .Concat(capitalizedTerms)
            .Select(term => term.Trim(' ', ':', '-', '.'))
            .Where(term => term.Length > 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(maxTerms)
            .ToList();
    }

    private static IEnumerable<(string Term, string Answer)> ExtractDefinitionCards(string text)
    {
        var pattern = @"(?<term>[A-ZA-Za-z][A-Za-z0-9\s\-]{2,80}?)\s+(is defined as|is|means|refers to|includes|consists of)\s+(?<answer>[^.!?]{12,260}[.!?]?)";
        return Regex.Matches(text, pattern, RegexOptions.IgnoreCase)
            .Select(match => (
                Term: match.Groups["term"].Value.Trim(),
                Answer: $"{match.Groups["term"].Value.Trim()} {match.Groups[2].Value} {match.Groups["answer"].Value.Trim()}"))
            .Where(item => item.Term.Length <= 80 && item.Answer.Length > item.Term.Length + 8);
    }

    private static List<string> ExtractHeadings(string text) =>
        Regex.Matches(text, @"(?:^|\s)(?:#{1,6}\s*)?(?<heading>[A-Z][A-Za-z0-9\s/&\-]{3,70})(?:\:|\s+-)")
            .Select(match => match.Groups["heading"].Value.Trim())
            .Where(item => item.Split(' ').Length <= 8)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();

    private static string FindFollowingText(string text, string heading)
    {
        var index = text.IndexOf(heading, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return heading;
        }

        var start = Math.Min(text.Length, index + heading.Length);
        return text[start..][..Math.Min(text.Length - start, 280)].Trim(' ', '-', ':');
    }

    private static string FindSentenceForTerm(string term, IReadOnlyList<string> sentences) =>
        sentences.FirstOrDefault(sentence => sentence.Contains(term, StringComparison.OrdinalIgnoreCase))
        ?? "Important term from the material.";

    private static string ExtractFocusTerm(string sentence)
    {
        var term = Regex.Match(sentence, @"\b[A-Z][A-Za-z0-9]*(?:\s+[A-Z][A-Za-z0-9]*){0,2}\b");
        if (term.Success)
        {
            return term.Value;
        }

        var words = Regex.Matches(sentence, @"[A-Za-z][A-Za-z0-9]{4,}")
            .Select(match => match.Value)
            .Take(3);
        return string.Join(" ", words).Trim();
    }

    private static void AddCard(List<FlashcardResult> cards, string question, string answer, int maxCards)
    {
        question = question.Trim();
        answer = answer.Trim();
        if (cards.Count >= Math.Clamp(maxCards, 1, 30) ||
            string.IsNullOrWhiteSpace(question) ||
            string.IsNullOrWhiteSpace(answer) ||
            cards.Any(card => card.Question.Equals(question, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        cards.Add(new FlashcardResult(question, answer.Length > 500 ? answer[..500].Trim() : answer));
    }

    private static void AppendBullets(StringBuilder builder, string heading, IEnumerable<string> items)
    {
        builder.AppendLine(heading);
        builder.AppendLine();
        foreach (var item in items.Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine(item.TrimStart().StartsWith("- ", StringComparison.Ordinal) ? item.Trim() : $"- {item.Trim()}");
        }
        builder.AppendLine();
    }
}
