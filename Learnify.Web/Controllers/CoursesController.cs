using AutoMapper;
using Learnify.Application;
using Learnify.Application.DTOs;
using Learnify.Core.Entities;
using Learnify.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Learnify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CoursesController> _logger;
    private readonly INotificationService _notificationService;

    public CoursesController(IUnitOfWork unitOfWork, IMapper mapper, ILogger<CoursesController> logger, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _notificationService = notificationService;
    }

    private Guid GetUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // GET: api/courses
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<CourseDto>>>> GetCourses()
    {
        try
        {
            var userId = GetUserId();
            var courses = await _unitOfWork.Courses.FindByUserIdAsync(userId);
            var courseDtos = _mapper.Map<IEnumerable<CourseDto>>(courses);
            return Ok(ApiResponse<IEnumerable<CourseDto>>.Ok(courseDtos, "Courses retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving courses.");
            return StatusCode(500, ApiResponse<IEnumerable<CourseDto>>.BadRequest("An error occurred while retrieving courses."));
        }
    }

    // GET: api/courses/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CourseDto>>> GetCourse(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var course = (await _unitOfWork.Courses.FindAsync(c => c.Id == id && c.UserId == userId))
                .FirstOrDefault();
            if (course == null)
                return NotFound(ApiResponse<CourseDto>.NotFound($"Course with ID {id} not found."));

            var courseDto = _mapper.Map<CourseDto>(course);
            return Ok(ApiResponse<CourseDto>.Ok(courseDto, "Course retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving course with ID {CourseId}", id);
            return StatusCode(500, ApiResponse<CourseDto>.BadRequest("An error occurred while retrieving the course."));
        }
    }

    // POST: api/courses
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CourseDto>>> CreateCourse([FromBody] CreateCourseDto createCourseDto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(createCourseDto.Title))
            {
                return BadRequest(ApiResponse<CourseDto>.BadRequest("Course title cannot be empty."));
            }

            var userId = GetUserId();
            var course = new Course
            {
                Id = Guid.NewGuid(),
                Title = createCourseDto.Title.Trim(),
                Description = createCourseDto.Description?.Trim() ?? string.Empty,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Courses.AddAsync(course);
            await _unitOfWork.SaveChangesAsync();

            var courseDto = _mapper.Map<CourseDto>(course);
            _logger.LogInformation("Course created for user {UserId}: {CourseId}", userId, courseDto.Id);

            return CreatedAtAction(nameof(GetCourse), new { id = courseDto.Id },
                ApiResponse<CourseDto>.Ok(courseDto, "Course created"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating course.");
            return StatusCode(500, ApiResponse<CourseDto>.BadRequest("An error occurred while creating the course."));
        }
    }

    // PUT: api/courses/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CourseDTO>>> UpdateCourse(Guid id, UpdateCourseDTO updateCourseDto)
    {
        try
        {
            var course = await _unitOfWork.Courses.GetByIdAsync(id);
            if (course == null || course.UserId != GetUserId())
            {
                return NotFound(ApiResponse<CourseDTO>.NotFound($"Course with ID {id} not found."));
            }

            if (string.IsNullOrWhiteSpace(updateCourseDto.Title))
            {
                return BadRequest(ApiResponse<CourseDTO>.BadRequest("Course title cannot be empty."));
            }

            course.Title = updateCourseDto.Title.Trim();
            course.Description = updateCourseDto.Description?.Trim() ?? string.Empty;

            _unitOfWork.Courses.Update(course);
            await _unitOfWork.SaveChangesAsync();

            var courseDto = _mapper.Map<CourseDTO>(course);
            return Ok(ApiResponse<CourseDTO>.Ok(courseDto, "Course updated successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating course with ID {CourseId}", id);
            return StatusCode(500, ApiResponse<CourseDTO>.BadRequest("An error occurred while updating the course."));
        }
    }

    // DELETE: api/courses/{id}
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteCourse(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var course = await _unitOfWork.Courses.GetByIdAsync(id);
            if (course == null)
            {
                return NotFound(ApiResponse.NotFound($"Course with ID {id} not found."));
            }

            if (course.UserId != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.BadRequest("You do not own this course"));
            }

            await _unitOfWork.Courses.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Course deleted for user {UserId}: {CourseId}", userId, id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting course with ID {CourseId}", id);
            return StatusCode(500, ApiResponse.BadRequest("An error occurred while deleting the course."));
        }
    }

    // POST: api/courses/{id}/enroll
    [HttpPost("{id:guid}/enroll")]
    public async Task<ActionResult<ApiResponse>> EnrollInCourse(Guid id, [FromBody] EnrollRequestDTO request)
    {
        try
        {
            var course = await _unitOfWork.Courses.GetByIdAsync(id);
            if (course == null)
                return NotFound(ApiResponse.NotFound($"Course with ID {id} not found."));

            // Queue enrollment confirmation email
            await _notificationService.QueueNotificationAsync(new NotificationMessage
            {
                NotificationType = "CourseEnrollment",
                Recipient = request.Email,
                RecipientName = request.Name,
                Subject = $"Enrolled in {course.Title}",
                Body = $"<h1>Enrollment Confirmed</h1><p>You have been enrolled in <strong>{course.Title}</strong>.</p><p>Start learning today!</p>",
                Metadata = new Dictionary<string, string?>
                {
                    ["courseId"] = id.ToString(),
                    ["courseTitle"] = course.Title
                },
                CreatedAt = DateTime.UtcNow
            });

            return Ok(ApiResponse.Ok(null, $"Successfully enrolled in '{course.Title}'. Confirmation email queued."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enrolling in course with ID {CourseId}", id);
            return StatusCode(500, ApiResponse.BadRequest("An error occurred while enrolling in the course."));
        }
    }
}

/// <summary>
/// Simple DTO for course enrollment requests.
/// </summary>
public class EnrollRequestDTO
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
