using Learnify.Core.Models;

namespace Learnify.Core.Interfaces;

public interface IDocumentTextExtractor
{
    Task<DocumentTextExtractionResult> ExtractAsync(
        string fileName,
        string contentType,
        byte[] bytes,
        CancellationToken cancellationToken = default);
}
