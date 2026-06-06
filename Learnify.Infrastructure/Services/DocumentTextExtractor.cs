using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Learnify.Core.Interfaces;
using Learnify.Core.Models;

namespace Learnify.Infrastructure.Services;

public sealed partial class DocumentTextExtractor : IDocumentTextExtractor
{
    private const string NoReadableTextMessage =
        "No readable text was extracted. Please upload a text-based PDF, .txt, .md, or .docx file.";

    private readonly IPdfTextExtractor _pdfTextExtractor;

    public DocumentTextExtractor(IPdfTextExtractor pdfTextExtractor)
    {
        _pdfTextExtractor = pdfTextExtractor;
    }

    public async Task<DocumentTextExtractionResult> ExtractTextAsync(
        string fileName,
        string contentType,
        Stream stream,
        CancellationToken ct = default)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension is not ".txt" and not ".md" and not ".pdf" and not ".docx")
        {
            throw new InvalidDataException("Unsupported file type. Upload .txt, .md, text-based .pdf, or .docx.");
        }

        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, ct);
        var bytes = memory.ToArray();

        var text = extension switch
        {
            ".txt" or ".md" => Encoding.UTF8.GetString(bytes),
            ".pdf" => await ExtractPdfTextAsync(bytes, ct),
            ".docx" => ExtractDocxText(bytes),
            _ => string.Empty
        };

        text = NormalizeText(text);
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidDataException(NoReadableTextMessage);
        }

        var warning = extension switch
        {
            ".pdf" => "PDF support requires selectable text. Scanned/image-only PDFs are not supported.",
            ".docx" => "DOCX support extracts plain text only.",
            _ => null
        };

        return new DocumentTextExtractionResult(
            Path.GetFileName(fileName),
            string.IsNullOrWhiteSpace(contentType) ? GuessContentType(extension) : contentType,
            text,
            text.Length,
            warning);
    }

    private static string ExtractDocxText(byte[] bytes)
    {
        try
        {
            using var memory = new MemoryStream(bytes);
            using var archive = new ZipArchive(memory, ZipArchiveMode.Read, leaveOpen: false);
            var document = archive.GetEntry("word/document.xml");
            if (document == null)
            {
                throw new InvalidDataException("The uploaded DOCX could not be parsed.");
            }

            var paragraphs = new List<string>();
            var current = new StringBuilder();
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };

            using var entryStream = document.Open();
            using var reader = XmlReader.Create(entryStream, settings);
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "t")
                {
                    var value = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        if (current.Length > 0)
                        {
                            current.Append(' ');
                        }

                        current.Append(value);
                    }
                }

                if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "p")
                {
                    var paragraph = NormalizeText(current.ToString());
                    if (!string.IsNullOrWhiteSpace(paragraph))
                    {
                        paragraphs.Add(paragraph);
                    }

                    current.Clear();
                }
            }

            if (current.Length > 0)
            {
                var paragraph = NormalizeText(current.ToString());
                if (!string.IsNullOrWhiteSpace(paragraph))
                {
                    paragraphs.Add(paragraph);
                }
            }

            return string.Join(Environment.NewLine, paragraphs);
        }
        catch (Exception ex) when (ex is not InvalidDataException)
        {
            throw new InvalidDataException("The uploaded DOCX could not be parsed.", ex);
        }
    }

    private async Task<string> ExtractPdfTextAsync(byte[] bytes, CancellationToken ct)
    {
        try
        {
            return await _pdfTextExtractor.ExtractTextAsync(bytes, ct);
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidDataException(NoReadableTextMessage, ex);
        }
    }

    private static string GuessContentType(string extension) =>
        extension switch
        {
            ".txt" => "text/plain",
            ".md" => "text/markdown",
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };

    private static string NormalizeText(string text)
        => WhitespaceRegex().Replace(text, " ").Trim();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
