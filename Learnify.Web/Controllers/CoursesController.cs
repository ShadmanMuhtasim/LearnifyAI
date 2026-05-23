using AutoMapper;
using Learnify.Application;
using Learnify.Application.DTOs;
using Learnify.Core.Entities;
using Learnify.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    // GET: api/courses
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<CourseDTO>>>> GetCourses()
    {
        try
        {
            var courses = await _unitOfWork.Courses.GetAllAsync();
            var courseDtos = _mapper.Map<IEnumerable<CourseDTO>>(courses);
            return Ok(ApiResponse<IEnumerable<CourseDTO>>.Ok(courseDtos, "Courses retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving courses.");
            return StatusCode(500, ApiResponse<IEnumerable<CourseDTO>>.BadRequest("An error occurred while retrieving courses."));
        }
    }

    // GET: api/courses/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CourseDTO>>> GetCourse(Guid id)
    {
        try
        {
            var course = await _unitOfWork.Courses.GetByIdAsync(id);
            if (course == null)
                return NotFound(ApiResponse<CourseDTO>.NotFound($"Course with ID {id} not found."));

            var courseDto = _mapper.Map<CourseDTO>(course);
            return Ok(ApiResponse<CourseDTO>.Ok(courseDto, "Course retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving course with ID {CourseId}", id);
            return StatusCode(500, ApiResponse<CourseDTO>.BadRequest("An error occurred while retrieving the course."));
        }
    }

    // POST: api/courses
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CourseDTO>>> CreateCourse(CreateCourseDTO createCourseDto)
    {
        try
        {
            var course = _mapper.Map<Course>(createCourseDto);
            await _unitOfWork.Courses.AddAsync(course);
            await _unitOfWork.SaveChangesAsync();

            var courseDto = _mapper.Map<CourseDTO>(course);

            // Queue a notification to all users about the new course
            var allUsers = await _unitOfWork.Users.GetAllAsync();
            foreach (var user in allUsers)
            {
                await _notificationService.QueueNotificationAsync(new NotificationMessage
                {
                    NotificationType = "NewCourseAlert",
                    Recipient = user.Email,
                    RecipientName = user.FullName,
                    Subject = $"New Course Available: {courseDto.Title}",
                    Body = $"<h1>New Course: {courseDto.Title}</h1><p>{courseDto.Description}</p><p>Enroll now on Learnify!</p>",
                    Metadata = new Dictionary<string, string?>
                    {
                        ["courseId"] = courseDto.Id.ToString(),
                        ["category"] = courseDto.Category
                    },
                    CreatedAt = DateTime.UtcNow
                });
            }

            _logger.LogInformation("New course notifications queued for {UserCount} users. Course: {Title}",
                allUsers.Count(), courseDto.Title);

            return CreatedAtAction(nameof(GetCourse), new { id = courseDto.Id },
                ApiResponse<CourseDTO>.Ok(courseDto, "Course created successfully. Enrollment notifications queued."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating course.");
            return StatusCode(500, ApiResponse<CourseDTO>.BadRequest("An error occurred while creating the course."));
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
