using Learnify.Application;
using Learnify.Application.DTOs;
using Learnify.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Learnify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuizzesController : ControllerBase
{
    private readonly IQuizService _quizService;
    private readonly ILogger<QuizzesController> _logger;

    public QuizzesController(IQuizService quizService, ILogger<QuizzesController> logger)
    {
        _quizService = quizService;
        _logger = logger;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<ApiResponse<QuizDto>>> Generate(
        [FromBody] GenerateQuizRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var quiz = await _quizService.GenerateQuizAsync(GetUserId(), request, cancellationToken);
            return CreatedAtAction(
                nameof(GetQuiz),
                new { id = quiz.Id },
                ApiResponse<QuizDto>.Ok(quiz, "Quiz generated successfully."));
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound(ApiResponse<QuizDto>.NotFound("Note not found."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<QuizDto>.BadRequest(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating quiz.");
            return StatusCode(500, ApiResponse<QuizDto>.BadRequest("Failed to generate quiz."));
        }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<QuizDto>>>> GetQuizzes()
    {
        var quizzes = await _quizService.GetQuizzesAsync(GetUserId());
        return Ok(ApiResponse<IReadOnlyList<QuizDto>>.Ok(quizzes, "Quizzes retrieved successfully."));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<QuizDto>>> GetQuiz(Guid id, [FromQuery] bool includeAnswers = false)
    {
        var quiz = await _quizService.GetQuizAsync(GetUserId(), id, includeAnswers);
        if (quiz == null)
        {
            return NotFound(ApiResponse<QuizDto>.NotFound("Quiz not found."));
        }

        return Ok(ApiResponse<QuizDto>.Ok(quiz, "Quiz retrieved successfully."));
    }

    [HttpPost("{id:guid}/attempts")]
    public async Task<ActionResult<ApiResponse<QuizResultDto>>> SubmitAttempt(
        Guid id,
        [FromBody] SubmitQuizAttemptRequest request)
    {
        try
        {
            var result = await _quizService.SubmitAttemptAsync(GetUserId(), id, request);
            if (result == null)
            {
                return NotFound(ApiResponse<QuizResultDto>.NotFound("Quiz not found."));
            }

            return Ok(ApiResponse<QuizResultDto>.Ok(result, "Quiz submitted successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<QuizResultDto>.BadRequest(ex.Message));
        }
    }

    [HttpGet("{id:guid}/attempts")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<QuizAttemptDto>>>> GetAttempts(Guid id)
    {
        var attempts = await _quizService.GetAttemptsAsync(GetUserId(), id);
        return Ok(ApiResponse<IReadOnlyList<QuizAttemptDto>>.Ok(attempts, "Quiz attempts retrieved successfully."));
    }

    private Guid GetUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
