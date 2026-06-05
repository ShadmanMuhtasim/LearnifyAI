using Learnify.Application;
using Learnify.Application.DTOs;
using Learnify.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Learnify.Web.Controllers;

[ApiController]
[Route("api/study-planner")]
[Authorize]
public class StudyPlannerController : ControllerBase
{
    private readonly IStudyPlannerService _studyPlannerService;
    private readonly ILogger<StudyPlannerController> _logger;

    public StudyPlannerController(
        IStudyPlannerService studyPlannerService,
        ILogger<StudyPlannerController> logger)
    {
        _studyPlannerService = studyPlannerService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StudyPlanItemDto>>>> GetItems(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var items = await _studyPlannerService.ListAsync(GetUserId(), from, to, status, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<StudyPlanItemDto>>.Ok(items, "Study plan items retrieved successfully."));
    }

    [HttpGet("upcoming")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StudyPlanItemDto>>>> GetUpcoming(CancellationToken cancellationToken)
    {
        var items = await _studyPlannerService.GetUpcomingAsync(GetUserId(), cancellationToken: cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<StudyPlanItemDto>>.Ok(items, "Upcoming study plan items retrieved successfully."));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<StudyPlanSummaryDto>>> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await _studyPlannerService.GetSummaryAsync(GetUserId(), cancellationToken);
        return Ok(ApiResponse<StudyPlanSummaryDto>.Ok(summary, "Study planner summary retrieved successfully."));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<StudyPlanItemDto>>> Create(
        [FromBody] CreateStudyPlanItemRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var item = await _studyPlannerService.CreateAsync(GetUserId(), request, cancellationToken);
            return CreatedAtAction(nameof(GetItems), new { id = item.Id },
                ApiResponse<StudyPlanItemDto>.Ok(item, "Study plan item created successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<StudyPlanItemDto>.BadRequest(ex.Message));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<StudyPlanItemDto>>> Update(
        Guid id,
        [FromBody] UpdateStudyPlanItemRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var item = await _studyPlannerService.UpdateAsync(GetUserId(), id, request, cancellationToken);
            if (item == null)
            {
                return NotFound(ApiResponse<StudyPlanItemDto>.NotFound("Study plan item not found."));
            }

            return Ok(ApiResponse<StudyPlanItemDto>.Ok(item, "Study plan item updated successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<StudyPlanItemDto>.BadRequest(ex.Message));
        }
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<ApiResponse<StudyPlanItemDto>>> Complete(Guid id, CancellationToken cancellationToken)
    {
        var item = await _studyPlannerService.CompleteAsync(GetUserId(), id, cancellationToken);
        if (item == null)
        {
            return NotFound(ApiResponse<StudyPlanItemDto>.NotFound("Study plan item not found."));
        }

        return Ok(ApiResponse<StudyPlanItemDto>.Ok(item, "Study plan item completed successfully."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _studyPlannerService.DeleteAsync(GetUserId(), id, cancellationToken);
        if (!deleted)
        {
            return NotFound(ApiResponse.NotFound("Study plan item not found."));
        }

        _logger.LogInformation("Study plan item {StudyPlanItemId} deleted.", id);
        return NoContent();
    }

    private Guid GetUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
