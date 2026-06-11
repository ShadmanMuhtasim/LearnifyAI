using Learnify.Core.Interfaces;
using Learnify.Core.Models;

namespace Learnify.Infrastructure.Services;

public sealed class OcrTextExtractor : IOcrTextExtractor
{
    public Task<DocumentTextExtractionResult> ExtractTextAsync(
        byte[] fileBytes,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new DocumentTextExtractionResult(
            string.Empty,
            "OcrUnavailable",
            "This PDF appears to be scanned or image-only. OCR is not available or could not extract readable text."));
    }
}
