using Learnify.Core.Models;

namespace Learnify.Core.Interfaces;

public interface ILocalStudyToolService
{
    string GenerateSummary(string content, string summaryDepth = "Balanced");

    IReadOnlyList<FlashcardResult> GenerateFlashcards(string content, int maxCards = 10);

    string GenerateStudyTips(string content);
}
