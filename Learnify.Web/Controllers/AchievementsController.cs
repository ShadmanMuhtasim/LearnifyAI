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
public class AchievementsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AchievementsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AchievementStatusDto>>>> GetAchievements(
        CancellationToken cancellationToken)
    {
        var achievements = await _analyticsService.GetAchievementsAsync(GetUserId(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AchievementStatusDto>>.Ok(achievements, "Achievements retrieved successfully."));
    }

    private Guid GetUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
