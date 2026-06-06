using Learnify.Core.Interfaces;
using Learnify.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learnify.Web.Controllers;

[ApiController]
[Route("api/materials")]
[Authorize]
public class MaterialsController : ControllerBase
{
    private const int MaxUploadBytes = 5 * 1024 * 1024;
    private const string NoReadableTextMessage =
        "No readable text was extracted. Please upload a text-based PDF, .txt, .md, or .docx file.";

    private readonly IDocumentTextExtractor _documentTextExtractor;
    private readonly ILogger<MaterialsController> _logger;

    public MaterialsController(
        IDocumentTextExtractor documentTextExtractor,
        ILogger<MaterialsController> logger)
    {
        _documentTextExtractor = documentTextExtractor;
        _logger = logger;
    }

    [HttpPost("extract-text")]
    [RequestSizeLimit(5_500_000)]
    public async Task<ActionResult<DocumentTextExtractionResult>> ExtractText(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { success = false, message = "A supported study material file is required." });
        }

        if (file.Length > MaxUploadBytes)
        {
            return BadRequest(new { success = false, message = "File too large. Maximum 5MB." });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _documentTextExtractor.ExtractTextAsync(
                file.FileName,
                file.ContentType,
                stream,
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidDataException ex)
        {
            _logger.LogWarning(
                ex,
                "Material text extraction failed. FileName={FileName}, ContentType={ContentType}, Length={Length}",
                file.FileName,
                file.ContentType,
                file.Length);
            var message = string.IsNullOrWhiteSpace(ex.Message) ? NoReadableTextMessage : ex.Message;
            return BadRequest(new { success = false, message });
        }
    }
}
