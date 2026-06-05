namespace Learnify.Core.Interfaces;

/// <summary>
/// Extracts selectable text from text-based PDF documents.
/// </summary>
public interface IPdfTextExtractor
{
    /// <summary>
    /// Extracts normalized text from PDF bytes.
    /// </summary>
    /// <param name="pdfBytes">Raw PDF file bytes.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Readable text extracted from the PDF.</returns>
    /// <exception cref="InvalidDataException">Thrown when the PDF cannot be parsed.</exception>
    Task<string> ExtractTextAsync(byte[] pdfBytes, CancellationToken ct = default);
}
