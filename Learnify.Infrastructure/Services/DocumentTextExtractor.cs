using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Learnify.Core.Interfaces;
using Learnify.Core.Models;

namespace Learnify.Infrastructure.Services;

public sealed class DocumentTextExtractor : IDocumentTextExtractor
{
    private const string OcrUnavailableMessage =
        "This PDF appears to be scanned or image-only. OCR is not available or could not extract readable text.";

    private readonly IPdfTextExtractor _pdfTextExtractor;

    public DocumentTextExtractor(IPdfTextExtractor pdfTextExtractor)
    {
        _pdfTextExtractor = pdfTextExtractor;
    }

    public async Task<DocumentTextExtractionResult> ExtractAsync(
        string fileName,
        string contentType,
        byte[] bytes,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension is ".txt" or ".md" || contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
        {
            return new DocumentTextExtractionResult(Encoding.UTF8.GetString(bytes).Trim());
        }

        if (extension == ".pdf" || contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            var text = await _pdfTextExtractor.ExtractTextAsync(bytes, cancellationToken);
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidDataException(OcrUnavailableMessage);
            }

            return new DocumentTextExtractionResult(text);
        }

        if (extension == ".docx" ||
            contentType.Equals("application/vnd.openxmlformats-officedocument.wordprocessingml.document", StringComparison.OrdinalIgnoreCase))
        {
            return new DocumentTextExtractionResult(ExtractDocxText(bytes));
        }

        throw new NotSupportedException("Supported upload formats are .txt, .md, text-based .pdf, and .docx.");
    }

    private static string ExtractDocxText(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        var entry = archive.GetEntry("word/document.xml")
            ?? throw new InvalidDataException("DOCX document text could not be read.");

        using var entryStream = entry.Open();
        var document = XDocument.Load(entryStream);
        XNamespace word = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var paragraphs = document.Descendants(word + "p")
            .Select(paragraph => string.Concat(paragraph.Descendants(word + "t").Select(text => text.Value)).Trim())
            .Where(text => !string.IsNullOrWhiteSpace(text));

        return string.Join(Environment.NewLine, paragraphs).Trim();
    }
}
