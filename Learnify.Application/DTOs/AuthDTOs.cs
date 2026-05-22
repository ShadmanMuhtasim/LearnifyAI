namespace Learnify.Application.DTOs;

/// <summary>
/// DTO for user registration.
/// </summary>
public class RegisterDTO
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string Role { get; set; } = "Student";
}

/// <summary>
/// DTO for user login.
/// </summary>
public class LoginDTO
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// DTO for authentication token response.
/// </summary>
public class AuthResponseDTO
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// DTO for refresh token request.
/// </summary>
public class RefreshTokenDTO
{
    public Guid UserId { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
}
