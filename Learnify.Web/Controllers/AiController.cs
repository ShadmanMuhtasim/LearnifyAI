using Learnify.Core.Interfaces;
using Learnify.Application.DTOs.AI;
using Learnify.Application.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Learnify.Web.Controllers;

/// <summary>
/// AI-powered endpoints for note summarization, flashcard generation, and study tips.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IAiService _aiService;
    private readonly ILogger<AiController> _logger;
    private readonly AiSettings _aiSettings;

    public AiController(
        IAiService aiService,
        ILogger<AiController> logger,
        IOptions<AiSettings> aiSettings)
    {
        _aiService = aiService;
        _logger = logger;
        _aiSettings = aiSettings.Value;
    }

    /// <summary>
    /// Summarize note content using AI.
    /// </summary>
    [HttpPost("summarize")]
    public async Task<IActionResult> Summarize(
        [FromBody] SummarizeNoteRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { success = false, message = "Note content cannot be empty." });
        }

        try
        {
            var summary = await _aiService.SummarizeNoteAsync(request.Content, cancellationToken);

            var response = new SummarizeNoteResponse(
                request.NoteId,
                summary,
                DateTime.UtcNow);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error summarizing note '{NoteId}'", request.NoteId);
            return StatusCode(500, new { success = false, message = "Failed to summarize note. Please try again later." });
        }
    }

    /// <summary>
    /// Generate flashcards from educational content using AI.
    /// </summary>
    [HttpPost("flashcards")]
    public async Task<IActionResult> GenerateFlashcards(
        [FromBody] FlashcardRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { success = false, message = "Content cannot be empty." });
        }

        try
        {
            var flashcards = await _aiService.GenerateFlashcardsAsync(
                request.Content,
                request.Count,
                cancellationToken);

            var response = new FlashcardResponse(
                request.NoteId,
                flashcards.Select(f => new FlashcardItem(f.Question, f.Answer)).ToList(),
                DateTime.UtcNow);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating flashcards for note '{NoteId}'", request.NoteId);
            return StatusCode(500, new { success = false, message = "Failed to generate flashcards. Please try again later." });
        }
    }

    /// <summary>
    /// Generate study tips for a given topic using AI.
    /// </summary>
    [HttpPost("study-tips")]
    public async Task<IActionResult> GetStudyTips(
        [FromBody] StudyTipsRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Topic))
        {
            return BadRequest(new { success = false, message = "Topic cannot be empty." });
        }

        try
        {
            var tips = await _aiService.GetStudyTipsAsync(request.Topic, cancellationToken);

            var response = new StudyTipsResponse(tips, DateTime.UtcNow);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating study tips for topic '{Topic}'", request.Topic);
            return StatusCode(500, new { success = false, message = "Failed to generate study tips. Please try again later." });
        }
    }

    /// <summary>
    /// Get the currently active AI provider and model configuration.
    /// </summary>
    [HttpGet("provider")]
    public IActionResult GetActiveProvider()
    {
        var activeProvider = _aiSettings.ActiveProvider;
        var model = activeProvider.ToLowerInvariant() switch
        {
            "gemini" => _aiSettings.Gemini.Model,
            "openai" => _aiSettings.OpenAi.Model,
            "ollama" => _aiSettings.Ollama.Model,
            "claude" => _aiSettings.Claude.Model,
            _ => "unknown"
        };

        return Ok(new { activeProvider, model });
    }
}