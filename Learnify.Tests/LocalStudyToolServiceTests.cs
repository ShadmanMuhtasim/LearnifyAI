using Learnify.Infrastructure.Services;
using Xunit;

namespace Learnify.Tests;

public sealed class LocalStudyToolServiceTests
{
    private readonly LocalStudyToolService _service = new();

    [Fact]
    public void LocalSummary_ReturnsRequiredSectionsAndPreservesImportantFacts()
    {
        var summary = _service.GenerateSummary(
            "Article 12 is defined as a constitutional rule for state action. " +
            "In 1997, the Supreme Court explained the exception. " +
            "Force = mass x acceleration is a formula students must remember.",
            "Detailed");

        Assert.Contains("## Overview", summary);
        Assert.Contains("## Key Points", summary);
        Assert.Contains("## Important Details", summary);
        Assert.Contains("## Important Terms", summary);
        Assert.Contains("## Must Remember", summary);
        Assert.Contains("## Short Revision Summary", summary);
        Assert.Contains("1997", summary);
        Assert.Contains("Force", summary);
    }

    [Fact]
    public void LocalFlashcards_GenerateFromDefinitionPatterns()
    {
        var cards = _service.GenerateFlashcards(
            "Photosynthesis is the process plants use to convert light into chemical energy. " +
            "Chlorophyll means the green pigment that captures light.",
            4);

        Assert.NotEmpty(cards);
        Assert.Contains(cards, card => card.Question.Contains("Photosynthesis", StringComparison.OrdinalIgnoreCase));
        Assert.All(cards, card =>
        {
            Assert.False(string.IsNullOrWhiteSpace(card.Question));
            Assert.False(string.IsNullOrWhiteSpace(card.Answer));
        });
    }

    [Fact]
    public void LocalStudyTips_ReturnRequiredContentSpecificSections()
    {
        var tips = _service.GenerateStudyTips(
            "Hash Map collisions are handled by separate chaining and open addressing. " +
            "Load factor affects performance because higher load creates more collisions.");

        Assert.Contains("## Active Recall Questions", tips);
        Assert.Contains("## Key Concepts to Master", tips);
        Assert.Contains("## Common Confusions", tips);
        Assert.Contains("## Mini Study Plan", tips);
        Assert.Contains("## Self-Test Questions", tips);
        Assert.Contains("Hash Map", tips);
    }
}
