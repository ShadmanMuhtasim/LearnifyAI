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
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<DashboardAnalyticsDto>>> GetDashboard(
        CancellationToken cancellationToken)
    {
        var dashboard = await _analyticsService.GetDashboardAsync(GetUserId(), cancellationToken);
        return Ok(ApiResponse<DashboardAnalyticsDto>.Ok(dashboard, "Dashboard analytics retrieved successfully."));
    }

    [HttpGet("quiz-performance")]
    public async Task<ActionResult<ApiResponse<QuizPerformanceDto>>> GetQuizPerformance(
        CancellationToken cancellationToken)
    {
        var performance = await _analyticsService.GetQuizPerformanceAsync(GetUserId(), cancellationToken);
        return Ok(ApiResponse<QuizPerformanceDto>.Ok(performance, "Quiz performance retrieved successfully."));
    }

    [HttpGet("activity")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RecentActivityDto>>>> GetActivity(
        CancellationToken cancellationToken)
    {
        var activity = await _analyticsService.GetRecentActivityAsync(GetUserId(), 20, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RecentActivityDto>>.Ok(activity, "Recent activity retrieved successfully."));
    }

    private Guid GetUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
