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
public class UsersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UsersController> _logger;
    private readonly INotificationService _notificationService;

    public UsersController(IUnitOfWork unitOfWork, IMapper mapper, ILogger<UsersController> logger, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _notificationService = notificationService;
    }

    // GET: api/users
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDTO>>>> GetUsers()
    {
        try
        {
            var users = await _unitOfWork.Users.GetAllAsync();
            var userDtos = _mapper.Map<IEnumerable<UserDTO>>(users);
            return Ok(ApiResponse<IEnumerable<UserDTO>>.Ok(userDtos, "Users retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users.");
            return StatusCode(500, ApiResponse<IEnumerable<UserDTO>>.BadRequest("An error occurred while retrieving users."));
        }
    }

    // GET: api/users/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDTO>>> GetUser(Guid id)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null)
                return NotFound(ApiResponse<UserDTO>.NotFound($"User with ID {id} not found."));

            var userDto = _mapper.Map<UserDTO>(user);
            return Ok(ApiResponse<UserDTO>.Ok(userDto, "User retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user with ID {UserId}", id);
            return StatusCode(500, ApiResponse<UserDTO>.BadRequest("An error occurred while retrieving the user."));
        }
    }

    // POST: api/users
    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDTO>>> CreateUser(CreateUserDTO createUserDto)
    {
        try
        {
            var user = _mapper.Map<User>(createUserDto);
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            var userDto = _mapper.Map<UserDTO>(user);

            // Queue a welcome email notification (fire-and-forget, async background processing)
            await _notificationService.QueueNotificationAsync(new NotificationMessage
            {
                NotificationType = "WelcomeEmail",
                Recipient = userDto.Email,
                RecipientName = userDto.FullName,
                Subject = "Welcome to Learnify!",
                Body = $"<h1>Welcome, {userDto.FullName}!</h1><p>Your account has been created successfully. You can now log in and start learning.</p>",
                Metadata = new Dictionary<string, string?>
                {
                    ["userId"] = userDto.Id.ToString(),
                    ["role"] = userDto.Role
                },
                CreatedAt = DateTime.UtcNow
            });

            _logger.LogInformation("Welcome email queued for user '{Email}'.", userDto.Email);

            return CreatedAtAction(nameof(GetUser), new { id = userDto.Id },
                ApiResponse<UserDTO>.Ok(userDto, "User created successfully. Welcome email queued."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user.");
            return StatusCode(500, ApiResponse<UserDTO>.BadRequest("An error occurred while creating the user."));
        }
    }
}