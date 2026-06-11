using Learnify.Core.Models;

namespace Learnify.Core.Interfaces;

public interface ILocalStudyToolService
{
    string Summarize(string content);

    IReadOnlyList<FlashcardResult> GenerateFlashcards(string content, int count);

    string GenerateStudyTips(string content);
}
