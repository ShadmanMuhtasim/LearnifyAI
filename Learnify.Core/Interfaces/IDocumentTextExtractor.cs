using Learnify.Core.Models;

namespace Learnify.Core.Interfaces;

public interface IDocumentTextExtractor
{
    Task<DocumentTextExtractionResult> ExtractTextAsync(
        string fileName,
        string contentType,
        Stream stream,
        CancellationToken ct = default);
}
