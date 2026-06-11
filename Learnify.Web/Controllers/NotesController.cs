using AutoMapper;
using Learnify.Application;
using Learnify.Application.DTOs;
using Learnify.Application.Interfaces;
using Learnify.Core.Entities;
using Learnify.Core.Interfaces;
using Learnify.Core.Models;
using Learnify.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

namespace Learnify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotesController : ControllerBase
{
    private const int MaxSimplePdfBytes = 5 * 1024 * 1024;
    private const string FileOnlyPdfContent =
        "This PDF was uploaded without text extraction. Use AI Analyze on a text-based PDF or upload .txt/.md content to generate AI study tools.";
    private const string NoReadableTextMessage =
        "No readable text was extracted. Please upload a text-based PDF, .txt, .md, or .docx file.";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<NotesController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly IDocumentTextExtractor _documentTextExtractor;
    private readonly IAnalyticsService _analyticsService;

    public NotesController(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<NotesController> logger,
        ApplicationDbContext context,
        IPdfTextExtractor pdfTextExtractor,
        IDocumentTextExtractor documentTextExtractor,
        IAnalyticsService analyticsService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _context = context;
        _pdfTextExtractor = pdfTextExtractor;
        _documentTextExtractor = documentTextExtractor;
        _analyticsService = analyticsService;
    }

    private Guid GetUserId()
        => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

    // GET: api/notes
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResultDTO<NoteListItemDTO>>>> GetNotes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? courseId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserId();
            var safePage = Math.Max(1, page);
            var safePageSize = Math.Clamp(pageSize, 1, 50);

            var query = _context.Notes
                .AsNoTracking()
                .Where(note => note.Course.UserId == userId);

            if (courseId.HasValue)
            {
                query = query.Where(note => note.CourseId == courseId.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var rows = await query
                .OrderByDescending(note => note.CreatedAt)
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize)
                .Select(note => new
                {
                    note.Id,
                    note.Content,
                    note.CourseId,
                    CourseTitle = note.Course.Title,
                    HasAttachments = note.Attachments.Any(),
                    note.CreatedAt
                })
                .ToListAsync(cancellationToken);

            var noteDtos = rows.Select(note => new NoteListItemDTO
            {
                Id = note.Id,
                Title = ExtractTitle(note.Content),
                Preview = BuildPreview(note.Content, 220),
                CourseId = note.CourseId,
                CourseTitle = note.CourseTitle,
                HasAttachments = note.HasAttachments,
                ExtractionStatus = GetExtractionStatus(note.Content, note.HasAttachments),
                CreatedAt = note.CreatedAt
            }).ToList();

            var result = new PagedResultDTO<NoteListItemDTO>
            {
                Items = noteDtos,
                Page = safePage,
                PageSize = safePageSize,
                TotalCount = totalCount,
                TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)safePageSize)
            };

            return Ok(ApiResponse<PagedResultDTO<NoteListItemDTO>>.Ok(result, "Notes retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notes.");
            return StatusCode(500, ApiResponse<PagedResultDTO<NoteListItemDTO>>.BadRequest("An error occurred while retrieving notes."));
        }
    }

    // GET: api/notes/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<NoteDetailDTO>>> GetNote(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var noteDto = await _context.Notes
                .AsNoTracking()
                .Where(note => note.Id == id && note.Course.UserId == userId)
                .Select(note => new NoteDetailDTO
                {
                    Id = note.Id,
                    Title = "",
                    Content = note.Content,
                    CourseId = note.CourseId,
                    CourseTitle = note.Course.Title,
                    CreatedAt = note.CreatedAt,
                    Attachments = note.Attachments
                        .OrderBy(attachment => attachment.CreatedAt)
                        .Select(attachment => new NoteAttachmentMetadataDTO
                        {
                            Id = attachment.Id,
                            Name = attachment.Name,
                            Type = attachment.Type,
                            SizeBytes = attachment.SizeBytes,
                            CreatedAt = attachment.CreatedAt
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (noteDto == null)
                return NotFound(ApiResponse<NoteDetailDTO>.NotFound($"Note with ID {id} not found."));

            noteDto.Title = ExtractTitle(noteDto.Content);

            return Ok(ApiResponse<NoteDetailDTO>.Ok(noteDto, "Note retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving note with ID {NoteId}", id);
            return StatusCode(500, ApiResponse<NoteDetailDTO>.BadRequest("An error occurred while retrieving the note."));
        }
    }

    // POST: api/notes
    [HttpPost]
    public async Task<ActionResult<ApiResponse<NoteDTO>>> CreateNote(CreateNoteDTO createNoteDto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(createNoteDto.Content))
                return BadRequest(ApiResponse<NoteDTO>.BadRequest("Note content cannot be empty."));

            var userId = GetUserId();
            var course = await _unitOfWork.Courses.GetByIdAsync(createNoteDto.CourseId);
            if (course == null || course.UserId != userId)
                return NotFound(ApiResponse<NoteDTO>.NotFound($"Course with ID {createNoteDto.CourseId} not found."));

            var note = new Note
            {
                Id = Guid.NewGuid(),
                Content = createNoteDto.Content,
                CourseId = createNoteDto.CourseId,
                CreatedAt = DateTime.UtcNow
            };

            // Add attachments
            foreach (var attachment in createNoteDto.Attachments)
            {
                note.Attachments.Add(new NoteAttachment
                {
                    Id = Guid.NewGuid(),
                    Name = attachment.Name,
                    Type = attachment.Type,
                    Base64 = attachment.Base64,
                    SizeBytes = attachment.SizeBytes > 0 ? attachment.SizeBytes : EstimateBase64SizeBytes(attachment.Base64),
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _unitOfWork.Notes.AddAsync(note);
            await _unitOfWork.SaveChangesAsync();
            await TrackAsync(userId, "NoteUploaded", "Note", note.Id);

            var noteDto = new NoteDTO
            {
                Id = note.Id,
                Content = note.Content,
                CourseId = note.CourseId,
                CourseTitle = course.Title,
                Attachments = note.Attachments.Select(a => new NoteAttachmentDTO
                {
                    Id = a.Id,
                    Name = a.Name,
                    Type = a.Type,
                    Base64 = a.Base64,
                    SizeBytes = a.SizeBytes,
                    CreatedAt = a.CreatedAt
                }).ToList(),
                CreatedAt = note.CreatedAt
            };

            return CreatedAtAction(nameof(GetNote), new { id = noteDto.Id },
                ApiResponse<NoteDTO>.Ok(noteDto, "Note created successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating note.");
            return StatusCode(500, ApiResponse<NoteDTO>.BadRequest("An error occurred while creating the note."));
        }
    }

    // PUT: api/notes/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<NoteDTO>>> UpdateNote(Guid id, UpdateNoteDTO updateNoteDto)
    {
        try
        {
            var userId = GetUserId();
            var note = await _context.Notes
                .Include(n => n.Course)
                .Include(n => n.Attachments)
                .FirstOrDefaultAsync(n => n.Id == id && n.Course.UserId == userId);

            if (note == null)
                return NotFound(ApiResponse<NoteDTO>.NotFound($"Note with ID {id} not found."));

            note.Content = updateNoteDto.Content;

            // --- Attachment synchronization ---
            // The frontend sends attachments with their DB GUIDs (for existing) or
            // client-generated UUIDs (for newly uploaded ones). We need to:
            // 1. Remove attachments that no longer exist in the payload (deleted by user).
            // 2. Update attachments whose ID matches an existing DB row.
            // 3. Add attachments whose ID does NOT match any existing row (new uploads).

            var incomingIds = updateNoteDto.Attachments.Select(a => a.Id).ToHashSet();

            // Remove attachments that were deleted from the payload
            var attachmentsToRemove = note.Attachments
                .Where(a => !incomingIds.Contains(a.Id))
                .ToList();
            foreach (var attachment in attachmentsToRemove)
            {
                note.Attachments.Remove(attachment);
            }

            // Update or add attachments
            foreach (var dtoAttachment in updateNoteDto.Attachments)
            {
                var existing = note.Attachments.FirstOrDefault(a => a.Id == dtoAttachment.Id);
                if (existing != null)
                {
                    // Existing attachment — update in place
                    existing.Name = dtoAttachment.Name;
                    existing.Type = dtoAttachment.Type;
                    if (!string.IsNullOrWhiteSpace(dtoAttachment.Base64))
                    {
                        existing.Base64 = dtoAttachment.Base64;
                        existing.SizeBytes = dtoAttachment.SizeBytes > 0
                            ? dtoAttachment.SizeBytes
                            : EstimateBase64SizeBytes(dtoAttachment.Base64);
                    }
                }
                else
                {
                    // New attachment (client-generated UUID from file upload)
                    note.Attachments.Add(new NoteAttachment
                    {
                        Id = dtoAttachment.Id,
                        Name = dtoAttachment.Name,
                        Type = dtoAttachment.Type,
                        Base64 = dtoAttachment.Base64,
                        SizeBytes = dtoAttachment.SizeBytes > 0
                            ? dtoAttachment.SizeBytes
                            : EstimateBase64SizeBytes(dtoAttachment.Base64),
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _unitOfWork.SaveChangesAsync();

            var noteDto = new NoteDTO
            {
                Id = note.Id,
                Content = note.Content,
                CourseId = note.CourseId,
                CourseTitle = note.Course?.Title,
                Attachments = note.Attachments.Select(a => new NoteAttachmentDTO
                {
                    Id = a.Id,
                    Name = a.Name,
                    Type = a.Type,
                    Base64 = a.Base64,
                    SizeBytes = a.SizeBytes,
                    CreatedAt = a.CreatedAt
                }).ToList(),
                CreatedAt = note.CreatedAt
            };

            return Ok(ApiResponse<NoteDTO>.Ok(noteDto, "Note updated successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating note with ID {NoteId}", id);
            return StatusCode(500, ApiResponse<NoteDTO>.BadRequest("An error occurred while updating the note."));
        }
    }

    // DELETE: api/notes/{id}
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteNote(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var notes = await _unitOfWork.Notes.FindByUserIdAsync(userId);
            var noteList = notes.ToList();
            var note = noteList.FirstOrDefault(n => n.Id == id);

            if (note == null)
                return NotFound(ApiResponse<bool>.NotFound($"Note with ID {id} not found."));

            _unitOfWork.Notes.Remove(note);
            await _unitOfWork.SaveChangesAsync();

            return Ok(ApiResponse<bool>.Ok(true, "Note deleted successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting note with ID {NoteId}", id);
            return StatusCode(500, ApiResponse<bool>.BadRequest("An error occurred while deleting the note."));
        }
    }

    // DELETE: api/notes/{noteId}/attachments/{attachmentId}
    [HttpDelete("{noteId:guid}/attachments/{attachmentId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteAttachment(Guid noteId, Guid attachmentId)
    {
        try
        {
            var userId = GetUserId();
            var note = await _context.Notes
                .Include(n => n.Course)
                .Include(n => n.Attachments)
                .FirstOrDefaultAsync(n => n.Id == noteId && n.Course.UserId == userId);

            if (note == null)
                return NotFound(ApiResponse<bool>.NotFound($"Note with ID {noteId} not found."));

            var attachment = note.Attachments.FirstOrDefault(a => a.Id == attachmentId);
            if (attachment == null)
                return NotFound(ApiResponse<bool>.NotFound($"Attachment with ID {attachmentId} not found."));

            note.Attachments.Remove(attachment);
            await _unitOfWork.SaveChangesAsync();

            return Ok(ApiResponse<bool>.Ok(true, "Attachment deleted successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attachment from note {NoteId}", noteId);
            return StatusCode(500, ApiResponse<bool>.BadRequest("An error occurred while deleting the attachment."));
        }
    }

    // GET: api/notes/{noteId}/attachments/{attachmentId}/download
    [HttpGet("{noteId:guid}/attachments/{attachmentId:guid}/download")]
    public async Task<IActionResult> DownloadAttachment(
        Guid noteId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var attachment = await _context.NoteAttachments
            .AsNoTracking()
            .Where(a => a.Id == attachmentId && a.NoteId == noteId && a.Note.Course.UserId == userId)
            .Select(a => new
            {
                a.Name,
                a.Type,
                a.Base64
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (attachment == null)
        {
            return NotFound(ApiResponse<bool>.NotFound("Attachment not found."));
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(attachment.Base64);
        }
        catch (FormatException)
        {
            return BadRequest(ApiResponse<bool>.BadRequest("Attachment content is invalid."));
        }

        return File(bytes, attachment.Type, attachment.Name);
    }

    // POST: api/notes/upload
    [HttpPost("upload")]
    [RequestSizeLimit(2_200_000)]
    public async Task<ActionResult<ApiResponse<NoteDTO>>> UploadTextNote(
        [FromForm] Guid courseId,
        [FromForm] IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse<NoteDTO>.BadRequest("A .txt or .md file is required."));
            }

            if (file.Length > 2 * 1024 * 1024)
            {
                return BadRequest(ApiResponse<NoteDTO>.BadRequest("File too large. Maximum 2MB."));
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension is not ".txt" and not ".md")
            {
                return BadRequest(ApiResponse<NoteDTO>.BadRequest("Only .txt and .md files are supported."));
            }

            var userId = GetUserId();
            var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
            if (course == null || course.UserId != userId)
            {
                return NotFound(ApiResponse<NoteDTO>.NotFound($"Course with ID {courseId} not found."));
            }

            string content;
            using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            {
                content = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return BadRequest(ApiResponse<NoteDTO>.BadRequest("Uploaded file does not contain any text."));
            }

            var note = new Note
            {
                Id = Guid.NewGuid(),
                Content = content,
                CourseId = course.Id,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Notes.AddAsync(note);
            await _unitOfWork.SaveChangesAsync();
            await TrackAsync(userId, "NoteUploaded", "Note", note.Id);

            var noteDto = new NoteDTO
            {
                Id = note.Id,
                Content = note.Content,
                CourseId = note.CourseId,
                CourseTitle = course.Title,
                Attachments = new List<NoteAttachmentDTO>(),
                CreatedAt = note.CreatedAt
            };

            return CreatedAtAction(nameof(GetNote), new { id = noteDto.Id },
                ApiResponse<NoteDTO>.Ok(noteDto, "Text note uploaded successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading text note.");
            return StatusCode(500, ApiResponse<NoteDTO>.BadRequest("An error occurred while uploading the note."));
        }
    }

    // POST: api/notes/upload-file
    [HttpPost("upload-file")]
    [RequestSizeLimit(5_500_000)]
    public async Task<ActionResult<ApiResponse<NoteDTO>>> UploadFileNote(
        [FromForm] Guid courseId,
        [FromForm] IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse<NoteDTO>.BadRequest("A PDF file is required."));
            }

            if (file.Length > MaxSimplePdfBytes)
            {
                return BadRequest(ApiResponse<NoteDTO>.BadRequest("File too large. Maximum 5MB."));
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".pdf")
            {
                return BadRequest(ApiResponse<NoteDTO>.BadRequest("Only PDF files are supported for simple file upload."));
            }

            var userId = GetUserId();
            var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
            if (course == null || course.UserId != userId)
            {
                return NotFound(ApiResponse<NoteDTO>.NotFound($"Course with ID {courseId} not found."));
            }

            string base64;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                base64 = Convert.ToBase64String(memoryStream.ToArray());
            }

            var contentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/pdf"
                : file.ContentType;

            var attachment = new NoteAttachment
            {
                Id = Guid.NewGuid(),
                Name = Path.GetFileName(file.FileName),
                Type = contentType,
                Base64 = base64,
                SizeBytes = file.Length,
                CreatedAt = DateTime.UtcNow
            };

            var note = new Note
            {
                Id = Guid.NewGuid(),
                Content = FileOnlyPdfContent,
                CourseId = course.Id,
                CreatedAt = DateTime.UtcNow,
                Attachments = new List<NoteAttachment> { attachment }
            };

            await _unitOfWork.Notes.AddAsync(note);
            await _unitOfWork.SaveChangesAsync();
            await TrackAsync(userId, "PdfUploaded", "Note", note.Id);

            var noteDto = new NoteDTO
            {
                Id = note.Id,
                Content = note.Content,
                CourseId = note.CourseId,
                CourseTitle = course.Title,
                Attachments = note.Attachments.Select(a => new NoteAttachmentDTO
                {
                    Id = a.Id,
                    Name = a.Name,
                    Type = a.Type,
                    Base64 = a.Base64,
                    SizeBytes = a.SizeBytes,
                    CreatedAt = a.CreatedAt
                }).ToList(),
                CreatedAt = note.CreatedAt
            };

            return CreatedAtAction(nameof(GetNote), new { id = noteDto.Id },
                ApiResponse<NoteDTO>.Ok(noteDto, "PDF uploaded successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading PDF file note.");
            return StatusCode(500, ApiResponse<NoteDTO>.BadRequest("An error occurred while uploading the PDF."));
        }
    }

    // POST: api/notes/upload-material
    [HttpPost("upload-material")]
    [RequestSizeLimit(5_500_000)]
    public async Task<ActionResult<ApiResponse<UploadMaterialResponse>>> UploadMaterial(
        [FromForm] Guid courseId,
        [FromForm] IFormFile file,
        [FromForm] string mode,
        [FromForm] string? title,
        [FromForm] string? generationMode,
        [FromForm] string? summaryDepth,
        [FromServices] IAiService aiService,
        CancellationToken cancellationToken)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse<UploadMaterialResponse>.BadRequest("Choose a .txt, .md, .pdf, or .docx file."));
            }

            if (file.Length > MaxSimplePdfBytes)
            {
                return BadRequest(ApiResponse<UploadMaterialResponse>.BadRequest("File too large. Maximum 5MB."));
            }

            var normalizedMode = NormalizeUploadMode(mode);
            var userId = GetUserId();
            var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
            if (course == null || course.UserId != userId)
            {
                return NotFound(ApiResponse<UploadMaterialResponse>.NotFound($"Course with ID {courseId} not found."));
            }

            var fileBytes = await ReadFileBytesAsync(file, cancellationToken);
            var attachment = CreateAttachment(file, fileBytes);
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var extractionStatus = "NotExtracted";
            string? warning = null;
            var extractedText = string.Empty;
            var aiUsed = false;
            string? generationModeUsed = null;

            if (normalizedMode == "SaveOnly")
            {
                if (extension is ".txt" or ".md")
                {
                    var extraction = await _documentTextExtractor.ExtractAsync(file.FileName, file.ContentType, fileBytes, cancellationToken);
                    extractedText = extraction.Text;
                    extractionStatus = string.IsNullOrWhiteSpace(extractedText) ? "Failed" : extraction.Status;
                    warning = extraction.Warning;
                }
            }
            else
            {
                var extraction = await _documentTextExtractor.ExtractAsync(file.FileName, file.ContentType, fileBytes, cancellationToken);
                extractedText = extraction.Text;
                extractionStatus = string.IsNullOrWhiteSpace(extractedText) ? extraction.Status : extraction.Status;
                warning = extraction.Warning;

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    return BadRequest(ApiResponse<UploadMaterialResponse>.BadRequest(warning ?? NoReadableTextMessage));
                }
            }

            var noteContent = BuildSavedNoteContent(
                normalizedMode,
                title,
                file.FileName,
                extractedText,
                warning);

            if (normalizedMode == "AiAnalyzeAndSave")
            {
                var analysis = await aiService.AnalyzeDocumentAsync(extractedText, file.FileName, cancellationToken);
                aiUsed = true;
                generationModeUsed = string.IsNullOrWhiteSpace(generationMode) ? "AIProvider" : generationMode.Trim();
                noteContent = BuildAnalyzedNoteContent(title, file.FileName, analysis.Summary, analysis.DetectedTopics, extractedText);
            }

            var note = new Note
            {
                Id = Guid.NewGuid(),
                Content = noteContent,
                CourseId = course.Id,
                CreatedAt = DateTime.UtcNow,
                Attachments = new List<NoteAttachment> { attachment }
            };

            await _unitOfWork.Notes.AddAsync(note);
            await _unitOfWork.SaveChangesAsync();
            await TrackAsync(userId, extension == ".pdf" ? "PdfUploaded" : "NoteUploaded", "Note", note.Id);

            var response = new UploadMaterialResponse(
                note.Id,
                string.IsNullOrWhiteSpace(title) ? Path.GetFileNameWithoutExtension(file.FileName) : title.Trim(),
                extractionStatus,
                extractedText.Length,
                note.Attachments.Count,
                warning,
                aiUsed,
                generationModeUsed,
                false,
                BuildUploadMessage(normalizedMode, extractionStatus));

            return CreatedAtAction(nameof(GetNote), new { id = note.Id },
                ApiResponse<UploadMaterialResponse>.Ok(response, response.Message));
        }
        catch (NotSupportedException ex)
        {
            return BadRequest(ApiResponse<UploadMaterialResponse>.BadRequest(ex.Message));
        }
        catch (InvalidDataException ex)
        {
            return BadRequest(ApiResponse<UploadMaterialResponse>.BadRequest(ex.Message));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Rate limit"))
        {
            return StatusCode(429, ApiResponse<UploadMaterialResponse>.BadRequest(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "AI provider could not analyze uploaded material '{FileName}'", file.FileName);
            return StatusCode(502, ApiResponse<UploadMaterialResponse>.BadRequest(ex.Message));
        }
    }

    // POST: api/notes/{noteId}/extract-attachment-text
    [HttpPost("{noteId:guid}/extract-attachment-text")]
    public async Task<ActionResult<ApiResponse<PostUploadExtractionResponse>>> ExtractAttachmentText(
        Guid noteId,
        CancellationToken cancellationToken)
    {
        try
        {
            var note = await GetOwnedNoteAsync(noteId);
            if (note == null)
            {
                return NotFound(ApiResponse<PostUploadExtractionResponse>.NotFound("Note not found."));
            }

            var extraction = await ExtractFirstAttachmentAsync(note, cancellationToken);
            if (string.IsNullOrWhiteSpace(extraction.Text))
            {
                return BadRequest(ApiResponse<PostUploadExtractionResponse>.BadRequest(extraction.Warning ?? NoReadableTextMessage));
            }

            note.Content = BuildSavedNoteContent("ExtractAndSave", null, note.Attachments.First().Name, extraction.Text, extraction.Warning);
            await _unitOfWork.SaveChangesAsync();

            var response = new PostUploadExtractionResponse(
                note.Id,
                extraction.Status,
                extraction.Text.Length,
                extraction.Warning,
                "Extracted text from the saved attachment.");

            return Ok(ApiResponse<PostUploadExtractionResponse>.Ok(response, response.Message));
        }
        catch (InvalidDataException ex)
        {
            return BadRequest(ApiResponse<PostUploadExtractionResponse>.BadRequest(ex.Message));
        }
    }

    // POST: api/notes/{noteId}/analyze-existing
    [HttpPost("{noteId:guid}/analyze-existing")]
    public async Task<ActionResult<ApiResponse<UploadMaterialResponse>>> AnalyzeExisting(
        Guid noteId,
        [FromBody] AnalyzeExistingRequest request,
        [FromServices] IAiService aiService,
        CancellationToken cancellationToken)
    {
        try
        {
            var note = await GetOwnedNoteAsync(noteId);
            if (note == null)
            {
                return NotFound(ApiResponse<UploadMaterialResponse>.NotFound("Note not found."));
            }

            var readableText = IsPlaceholderContent(note.Content) ? string.Empty : note.Content;
            string? warning = null;
            var extractionStatus = string.IsNullOrWhiteSpace(readableText) ? "NotExtracted" : "Extracted";

            if (string.IsNullOrWhiteSpace(readableText))
            {
                var extraction = await ExtractFirstAttachmentAsync(note, cancellationToken);
                if (string.IsNullOrWhiteSpace(extraction.Text))
                {
                    return BadRequest(ApiResponse<UploadMaterialResponse>.BadRequest(extraction.Warning ?? NoReadableTextMessage));
                }

                readableText = extraction.Text;
                warning = extraction.Warning;
                extractionStatus = extraction.Status;
            }

            var attachmentName = note.Attachments.FirstOrDefault()?.Name ?? "saved note";
            var analysis = await aiService.AnalyzeDocumentAsync(readableText, attachmentName, cancellationToken);
            note.Content = BuildAnalyzedNoteContent(null, attachmentName, analysis.Summary, analysis.DetectedTopics, readableText);
            await _unitOfWork.SaveChangesAsync();

            var response = new UploadMaterialResponse(
                note.Id,
                attachmentName,
                extractionStatus,
                readableText.Length,
                note.Attachments.Count,
                warning,
                true,
                string.IsNullOrWhiteSpace(request.GenerationMode) ? "AIProvider" : request.GenerationMode,
                false,
                "Analyzed the saved file using extracted text.");

            return Ok(ApiResponse<UploadMaterialResponse>.Ok(response, response.Message));
        }
        catch (InvalidDataException ex)
        {
            return BadRequest(ApiResponse<UploadMaterialResponse>.BadRequest(ex.Message));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Rate limit"))
        {
            return StatusCode(429, ApiResponse<UploadMaterialResponse>.BadRequest(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "AI provider could not analyze existing note '{NoteId}'", noteId);
            return StatusCode(502, ApiResponse<UploadMaterialResponse>.BadRequest(ex.Message));
        }
    }

    // POST: api/notes/analyze-upload
    [HttpPost("analyze-upload")]
    public async Task<ActionResult<ApiResponse<AnalyzeUploadResponse>>> AnalyzeAndSave(
        [FromBody] AnalyzeUploadRequest request,
        [FromServices] IAiService aiService,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.FileBase64))
            {
                return BadRequest(ApiResponse<AnalyzeUploadResponse>.BadRequest("File content is required."));
            }

            if (request.FileBase64.Length > 7_000_000)
            {
                return BadRequest(ApiResponse<AnalyzeUploadResponse>.BadRequest("File too large. Maximum 5MB."));
            }

            var userId = GetUserId();
            var fileBytes = Convert.FromBase64String(request.FileBase64);

            var contentForAi = await ExtractAnalyzeUploadTextAsync(request, fileBytes, cancellationToken);

            if (string.IsNullOrWhiteSpace(contentForAi))
            {
                return BadRequest(ApiResponse<AnalyzeUploadResponse>.BadRequest(NoReadableTextMessage));
            }

            var analysis = await aiService.AnalyzeDocumentAsync(
                contentForAi,
                request.FileName,
                cancellationToken);

            var courseName = !string.IsNullOrWhiteSpace(request.PreferredCourseName)
                ? request.PreferredCourseName.Trim()
                : analysis.SuggestedCourseName.Trim();

            if (string.IsNullOrWhiteSpace(courseName))
            {
                courseName = "General Notes";
            }

            var extractedSourceSection = request.FileType.Equals("pdf", StringComparison.OrdinalIgnoreCase)
                ? $"## Extracted PDF Text\n{contentForAi}\n\n"
                : string.Empty;

            var userCourses = (await _unitOfWork.Courses.FindByUserIdAsync(userId)).ToList();
            var existingCourse = userCourses.FirstOrDefault(c =>
                string.Equals(c.Title, courseName, StringComparison.OrdinalIgnoreCase));

            var courseWasCreated = false;
            Course course;

            if (existingCourse != null)
            {
                course = existingCourse;
            }
            else
            {
                course = new Course
                {
                    Id = Guid.NewGuid(),
                    Title = courseName,
                    Description = analysis.Summary,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Courses.AddAsync(course);
                courseWasCreated = true;
            }

            var noteContent = $"## AI Summary\n{analysis.Summary}\n\n" +
                              (analysis.DetectedTopics.Any()
                                  ? $"## Detected Topics\n{string.Join("\n", analysis.DetectedTopics.Select(topic => $"- {topic}"))}\n\n"
                                  : string.Empty) +
                              extractedSourceSection +
                              $"## Source File\n{request.FileName}";

            var note = new Note
            {
                Id = Guid.NewGuid(),
                Content = noteContent,
                CourseId = course.Id,
                CreatedAt = DateTime.UtcNow,
                Attachments = new List<NoteAttachment>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = request.FileName,
                        Type = request.FileType,
                        Base64 = request.FileBase64,
                        SizeBytes = fileBytes.LongLength,
                        CreatedAt = DateTime.UtcNow
                    }
                }
            };

            await _unitOfWork.Notes.AddAsync(note);
            await _unitOfWork.SaveChangesAsync();
            await TrackAsync(userId, request.FileType.Equals("pdf", StringComparison.OrdinalIgnoreCase) ? "PdfUploaded" : "NoteUploaded", "Note", note.Id);

            var result = new AnalyzeUploadResponse(
                NoteId: note.Id,
                CourseId: course.Id,
                CourseName: course.Title,
                CourseWasCreated: courseWasCreated,
                Summary: analysis.Summary,
                DetectedTopics: analysis.DetectedTopics,
                Message: courseWasCreated
                    ? $"Created new course '{course.Title}' and saved your note."
                    : $"Note saved under existing course '{course.Title}'.");

            return Ok(ApiResponse<AnalyzeUploadResponse>.Ok(result, result.Message));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Rate limit"))
        {
            return StatusCode(429, ApiResponse<AnalyzeUploadResponse>.BadRequest(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "AI provider could not analyze file '{FileName}'", request.FileName);
            return StatusCode(502, ApiResponse<AnalyzeUploadResponse>.BadRequest(ex.Message));
        }
        catch (FormatException)
        {
            return BadRequest(ApiResponse<AnalyzeUploadResponse>.BadRequest("Invalid base64 file content."));
        }
        catch (InvalidDataException)
        {
            return BadRequest(ApiResponse<AnalyzeUploadResponse>.BadRequest(NoReadableTextMessage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AnalyzeAndSave for file '{FileName}'", request.FileName);
            return StatusCode(500, ApiResponse<AnalyzeUploadResponse>.BadRequest("Failed to analyze and save document."));
        }
    }

    private async Task<string> ExtractAnalyzeUploadTextAsync(
        AnalyzeUploadRequest request,
        byte[] fileBytes,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        var fileType = request.FileType.Trim().ToLowerInvariant();

        if (extension is ".txt" or ".md" || fileType == "text")
        {
            if (extension is not ".txt" and not ".md" && fileType != "text")
            {
                return string.Empty;
            }

            return Encoding.UTF8.GetString(fileBytes).Trim();
        }

        if (extension == ".pdf" || fileType == "pdf" || fileType == "application/pdf")
        {
            return await _pdfTextExtractor.ExtractTextAsync(fileBytes, cancellationToken);
        }

        return string.Empty;
    }

    private async Task<Note?> GetOwnedNoteAsync(Guid noteId)
    {
        var userId = GetUserId();
        return await _context.Notes
            .Include(note => note.Course)
            .Include(note => note.Attachments)
            .FirstOrDefaultAsync(note => note.Id == noteId && note.Course.UserId == userId);
    }

    private async Task<DocumentTextExtractionResult> ExtractFirstAttachmentAsync(
        Note note,
        CancellationToken cancellationToken)
    {
        var attachment = note.Attachments.FirstOrDefault()
            ?? throw new InvalidDataException("This note does not have an attachment to extract.");

        var fileBytes = Convert.FromBase64String(attachment.Base64);
        return await _documentTextExtractor.ExtractAsync(
            attachment.Name,
            attachment.Type,
            fileBytes,
            cancellationToken);
    }

    private static async Task<byte[]> ReadFileBytesAsync(IFormFile file, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }

    private static NoteAttachment CreateAttachment(IFormFile file, byte[] fileBytes) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = Path.GetFileName(file.FileName),
            Type = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            Base64 = Convert.ToBase64String(fileBytes),
            SizeBytes = fileBytes.LongLength,
            CreatedAt = DateTime.UtcNow
        };

    private static string NormalizeUploadMode(string mode) =>
        mode.Trim().ToLowerInvariant() switch
        {
            "extractandsave" or "extract" => "ExtractAndSave",
            "aianalyzeandsave" or "aianalyze" or "analyze" => "AiAnalyzeAndSave",
            _ => "SaveOnly"
        };

    private static bool IsPlaceholderContent(string content) =>
        string.IsNullOrWhiteSpace(content) ||
        content.Trim().Equals(FileOnlyPdfContent, StringComparison.OrdinalIgnoreCase) ||
        content.Contains("uploaded without text extraction", StringComparison.OrdinalIgnoreCase);

    private static string BuildSavedNoteContent(
        string mode,
        string? title,
        string fileName,
        string extractedText,
        string? warning)
    {
        var heading = string.IsNullOrWhiteSpace(title)
            ? Path.GetFileNameWithoutExtension(fileName)
            : title.Trim();

        if (mode == "SaveOnly" && string.IsNullOrWhiteSpace(extractedText))
        {
            return FileOnlyPdfContent;
        }

        var warningSection = string.IsNullOrWhiteSpace(warning)
            ? string.Empty
            : $"## Extraction Warning\n{warning}\n\n";

        return $"# {heading}\n\n" +
               warningSection +
               $"## Extracted Text\n{extractedText.Trim()}\n\n" +
               $"## Source File\n{fileName}";
    }

    private static string BuildAnalyzedNoteContent(
        string? title,
        string fileName,
        string summary,
        List<string> detectedTopics,
        string extractedText)
    {
        var heading = string.IsNullOrWhiteSpace(title)
            ? Path.GetFileNameWithoutExtension(fileName)
            : title.Trim();

        return $"# {heading}\n\n" +
               $"## AI Summary\n{summary}\n\n" +
               (detectedTopics.Any()
                   ? $"## Detected Topics\n{string.Join("\n", detectedTopics.Select(topic => $"- {topic}"))}\n\n"
                   : string.Empty) +
               $"## Extracted Text\n{extractedText.Trim()}\n\n" +
               $"## Source File\n{fileName}";
    }

    private static string BuildUploadMessage(string mode, string extractionStatus) =>
        mode switch
        {
            "ExtractAndSave" => extractionStatus == "OcrExtracted"
                ? "Extracted text with OCR and saved the note."
                : "Extracted text and saved the note.",
            "AiAnalyzeAndSave" => "Analyzed extracted text and saved the note.",
            _ => "Saved the file without AI analysis."
        };

    private static string ExtractTitle(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return "Untitled Note";
        }

        var firstMeaningfulLine = content
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(line => !line.StartsWith("## ", StringComparison.Ordinal));

        if (string.IsNullOrWhiteSpace(firstMeaningfulLine))
        {
            return "Untitled Note";
        }

        var title = firstMeaningfulLine.Trim().TrimStart('#').Trim();
        return string.IsNullOrWhiteSpace(title)
            ? "Untitled Note"
            : title.Length <= 100 ? title : $"{title[..100]}...";
    }

    private static string BuildPreview(string content, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var compact = string.Join(
            " ",
            content.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Trim();

        return compact.Length <= maxLength ? compact : $"{compact[..maxLength]}...";
    }

    private static string GetExtractionStatus(string content, bool hasAttachments)
    {
        if (IsPlaceholderContent(content) && hasAttachments)
        {
            return "FileOnly";
        }

        if (content.Contains("## AI Summary", StringComparison.OrdinalIgnoreCase))
        {
            return "Analyzed";
        }

        if (content.Contains("## Extracted Text", StringComparison.OrdinalIgnoreCase))
        {
            return "Extracted";
        }

        return string.IsNullOrWhiteSpace(content) ? "Empty" : "TextReady";
    }

    private static long EstimateBase64SizeBytes(string base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
        {
            return 0;
        }

        var padding = base64.EndsWith("==", StringComparison.Ordinal) ? 2 :
            base64.EndsWith("=", StringComparison.Ordinal) ? 1 : 0;
        return Math.Max(0, (base64.Length * 3L / 4L) - padding);
    }

    private async Task TrackAsync(Guid userId, string activityType, string entityType, Guid entityId)
    {
        try
        {
            await _analyticsService.TrackAsync(userId, activityType, entityType, entityId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Analytics tracking failed for {ActivityType}", activityType);
        }
    }
}

public record AnalyzeUploadRequest(
    string FileName,
    string FileBase64,
    string FileType,
    string? PreferredCourseName);

public record AnalyzeUploadResponse(
    Guid NoteId,
    Guid CourseId,
    string CourseName,
    bool CourseWasCreated,
    string Summary,
    List<string> DetectedTopics,
    string Message);

public record UploadMaterialResponse(
    Guid NoteId,
    string Title,
    string ExtractionStatus,
    int CharacterCount,
    int AttachmentCount,
    string? Warning,
    bool AiUsed,
    string? GenerationModeUsed,
    bool? FromCache,
    string Message);

public record PostUploadExtractionResponse(
    Guid NoteId,
    string ExtractionStatus,
    int CharacterCount,
    string? Warning,
    string Message);

public record AnalyzeExistingRequest(
    string? GenerationMode = "Auto",
    string? SummaryDepth = "Balanced");
