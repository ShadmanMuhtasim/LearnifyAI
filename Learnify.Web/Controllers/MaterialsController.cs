using Learnify.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learnify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MaterialsController : ControllerBase
{
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
    public async Task<IActionResult> ExtractText(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { success = false, message = "Upload a .txt, .md, text-based .pdf, or .docx file first." });
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            return BadRequest(new { success = false, message = "File too large. Maximum 5MB." });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, cancellationToken);
            var result = await _documentTextExtractor.ExtractAsync(
                file.FileName,
                file.ContentType,
                memory.ToArray(),
                cancellationToken);

            if (string.IsNullOrWhiteSpace(result.Text))
            {
                return BadRequest(new { success = false, message = "No readable text was extracted. Please upload a text-based PDF, .txt, .md, or .docx file." });
            }

            return Ok(new
            {
                text = result.Text,
                result.Warning,
                fileName = Path.GetFileName(file.FileName)
            });
        }
        catch (NotSupportedException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (InvalidDataException ex)
        {
            _logger.LogWarning(ex, "Could not extract text from uploaded material {FileName}", file.FileName);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
