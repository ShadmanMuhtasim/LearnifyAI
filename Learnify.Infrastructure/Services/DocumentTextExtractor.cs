using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Learnify.Core.Interfaces;
using Learnify.Core.Models;

namespace Learnify.Infrastructure.Services;

public sealed class DocumentTextExtractor : IDocumentTextExtractor
{
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly IOcrTextExtractor _ocrTextExtractor;

    public DocumentTextExtractor(
        IPdfTextExtractor pdfTextExtractor,
        IOcrTextExtractor ocrTextExtractor)
    {
        _pdfTextExtractor = pdfTextExtractor;
        _ocrTextExtractor = ocrTextExtractor;
    }

    public async Task<DocumentTextExtractionResult> ExtractAsync(
        string fileName,
        string contentType,
        byte[] fileBytes,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (extension is ".txt" or ".md" || contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
        {
            return new DocumentTextExtractionResult(
                Encoding.UTF8.GetString(fileBytes).Trim(),
                "Extracted");
        }

        if (extension == ".docx" ||
            contentType.Equals("application/vnd.openxmlformats-officedocument.wordprocessingml.document", StringComparison.OrdinalIgnoreCase))
        {
            return new DocumentTextExtractionResult(ExtractDocxText(fileBytes), "Extracted");
        }

        if (extension == ".pdf" || contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            string text;
            try
            {
                text = await _pdfTextExtractor.ExtractTextAsync(fileBytes, cancellationToken);
            }
            catch (InvalidDataException)
            {
                return await _ocrTextExtractor.ExtractTextAsync(fileBytes, fileName, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                return new DocumentTextExtractionResult(text, "Extracted");
            }

            return await _ocrTextExtractor.ExtractTextAsync(fileBytes, fileName, cancellationToken);
        }

        throw new NotSupportedException("Supported formats are .txt, .md, .pdf, and .docx.");
    }

    private static string ExtractDocxText(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        var documentEntry = archive.GetEntry("word/document.xml")
            ?? throw new InvalidDataException("DOCX document text could not be read.");

        using var documentStream = documentEntry.Open();
        var document = XDocument.Load(documentStream);
        XNamespace word = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

        return string.Join(
            Environment.NewLine,
            document.Descendants(word + "p")
                .Select(paragraph => string.Concat(paragraph.Descendants(word + "t").Select(text => text.Value)).Trim())
                .Where(text => !string.IsNullOrWhiteSpace(text)))
            .Trim();
    }
}
