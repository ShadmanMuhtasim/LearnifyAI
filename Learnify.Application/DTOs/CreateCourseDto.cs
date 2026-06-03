using System.ComponentModel.DataAnnotations;

namespace Learnify.Application.DTOs;

public class CreateCourseDto
{
    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}
