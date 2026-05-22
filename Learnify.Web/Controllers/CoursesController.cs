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

    public CoursesController(IUnitOfWork unitOfWork, IMapper mapper, ILogger<CoursesController> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
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
            return CreatedAtAction(nameof(GetCourse), new { id = courseDto.Id },
                ApiResponse<CourseDTO>.Ok(courseDto, "Course created successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating course.");
            return StatusCode(500, ApiResponse<CourseDTO>.BadRequest("An error occurred while creating the course."));
        }
    }
}