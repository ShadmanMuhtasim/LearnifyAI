using System.Text;
using System.Text.RegularExpressions;
using Learnify.Core.Interfaces;
using UglyToad.PdfPig;

namespace Learnify.Infrastructure.Services;

public sealed partial class PdfTextExtractor : IPdfTextExtractor
{
    public Task<string> ExtractTextAsync(byte[] pdfBytes, CancellationToken ct = default)
    {
        if (pdfBytes.Length == 0)
        {
            return Task.FromResult(string.Empty);
        }

        try
        {
            using var document = PdfDocument.Open(pdfBytes);
            var builder = new StringBuilder();

            foreach (var page in document.GetPages())
            {
                ct.ThrowIfCancellationRequested();

                var text = NormalizeWhitespace(page.Text);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    if (builder.Length > 0)
                    {
                        builder.AppendLine();
                        builder.AppendLine();
                    }

                    builder.Append(text);
                }
            }

            return Task.FromResult(builder.ToString().Trim());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidDataException("The uploaded PDF could not be parsed.", ex);
        }
    }

    private static string NormalizeWhitespace(string text)
        => WhitespaceRegex().Replace(text, " ").Trim();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
