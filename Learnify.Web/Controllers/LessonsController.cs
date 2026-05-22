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
public class LessonsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<LessonsController> _logger;

    public LessonsController(IUnitOfWork unitOfWork, IMapper mapper, ILogger<LessonsController> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    // GET: api/lessons
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<LessonDTO>>>> GetLessons()
    {
        try
        {
            var lessons = await _unitOfWork.Lessons.GetAllAsync();
            var lessonDtos = _mapper.Map<IEnumerable<LessonDTO>>(lessons);
            return Ok(ApiResponse<IEnumerable<LessonDTO>>.Ok(lessonDtos, "Lessons retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving lessons.");
            return StatusCode(500, ApiResponse<IEnumerable<LessonDTO>>.BadRequest("An error occurred while retrieving lessons."));
        }
    }

    // GET: api/lessons/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<LessonDTO>>> GetLesson(Guid id)
    {
        try
        {
            var lesson = await _unitOfWork.Lessons.GetByIdAsync(id);
            if (lesson == null)
                return NotFound(ApiResponse<LessonDTO>.NotFound($"Lesson with ID {id} not found."));

            var lessonDto = _mapper.Map<LessonDTO>(lesson);
            return Ok(ApiResponse<LessonDTO>.Ok(lessonDto, "Lesson retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving lesson with ID {LessonId}", id);
            return StatusCode(500, ApiResponse<LessonDTO>.BadRequest("An error occurred while retrieving the lesson."));
        }
    }

    // GET: api/courses/{courseId}/lessons
    [HttpGet("courses/{courseId:guid}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<LessonDTO>>>> GetLessonsByCourse(Guid courseId)
    {
        try
        {
            var lessons = await _unitOfWork.Lessons.FindAsync(l => l.CourseId == courseId);
            var lessonDtos = _mapper.Map<IEnumerable<LessonDTO>>(lessons);
            return Ok(ApiResponse<IEnumerable<LessonDTO>>.Ok(lessonDtos, "Lessons retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving lessons for course {CourseId}", courseId);
            return StatusCode(500, ApiResponse<IEnumerable<LessonDTO>>.BadRequest("An error occurred while retrieving lessons."));
        }
    }

    // POST: api/lessons
    [HttpPost]
    public async Task<ActionResult<ApiResponse<LessonDTO>>> CreateLesson(CreateLessonDTO createLessonDto)
    {
        try
        {
            var course = await _unitOfWork.Courses.GetByIdAsync(createLessonDto.CourseId);
            if (course == null)
                return NotFound(ApiResponse<LessonDTO>.NotFound($"Course with ID {createLessonDto.CourseId} not found."));

            var lesson = _mapper.Map<Lesson>(createLessonDto);
            await _unitOfWork.Lessons.AddAsync(lesson);
            await _unitOfWork.SaveChangesAsync();

            var lessonDto = _mapper.Map<LessonDTO>(lesson);
            return CreatedAtAction(nameof(GetLesson), new { id = lessonDto.Id },
                ApiResponse<LessonDTO>.Ok(lessonDto, "Lesson created successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating lesson.");
            return StatusCode(500, ApiResponse<LessonDTO>.BadRequest("An error occurred while creating the lesson."));
        }
    }

    // PUT: api/lessons/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<LessonDTO>>> UpdateLesson(Guid id, UpdateLessonDTO updateLessonDto)
    {
        try
        {
            var lesson = await _unitOfWork.Lessons.GetByIdAsync(id);
            if (lesson == null)
                return NotFound(ApiResponse<LessonDTO>.NotFound($"Lesson with ID {id} not found."));

            _mapper.Map(updateLessonDto, lesson);
            _unitOfWork.Lessons.Update(lesson);
            await _unitOfWork.SaveChangesAsync();

            var lessonDto = _mapper.Map<LessonDTO>(lesson);
            return Ok(ApiResponse<LessonDTO>.Ok(lessonDto, "Lesson updated successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating lesson with ID {LessonId}", id);
            return StatusCode(500, ApiResponse<LessonDTO>.BadRequest("An error occurred while updating the lesson."));
        }
    }

    // DELETE: api/lessons/{id}
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteLesson(Guid id)
    {
        try
        {
            var lesson = await _unitOfWork.Lessons.GetByIdAsync(id);
            if (lesson == null)
                return NotFound(ApiResponse<object>.NotFound($"Lesson with ID {id} not found."));

            _unitOfWork.Lessons.Remove(lesson);
            await _unitOfWork.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(null, "Lesson deleted successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting lesson with ID {LessonId}", id);
            return StatusCode(500, ApiResponse<object>.BadRequest("An error occurred while deleting the lesson."));
        }
    }
}