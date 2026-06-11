using Learnify.Core.Models;

namespace Learnify.Core.Interfaces;

public interface IOcrTextExtractor
{
    Task<DocumentTextExtractionResult> ExtractTextAsync(
        byte[] fileBytes,
        string fileName,
        CancellationToken cancellationToken = default);
}
